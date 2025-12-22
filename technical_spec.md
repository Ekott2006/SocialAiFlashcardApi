# SocialAiFlashcard Technical Specification

## 1. System Overview
**SocialAiFlashcard** is a progressive web application designed to revolutionize flashcard study by integrating Generative AI for grading, robust social features, and a flexible Spaced Repetition System (SRS).

### 1.1. Core Philosophy
-   **AI-First Grading**: Users are not trusted to grade themselves. The AI acts as the "Teacher," evaluating nuances in natural language answers.
-   **Note-Card Separation**: Adopts the Anki data model where a `Note` (data) generates one or more `Cards` (views).
-   **Blocking Review Loop**: To ensure quality, the user must wait for the AI's feedback before proceeding, mimicking a real oral exam.
-   **Asynchronous Processing**: All heavy lifting (Card generation, Grading, Imports) is offloaded to background queues to keep the API responsive.

## 2. API Architecture
-   **Protocol**: HTTP/2.0 (REST) for actions, WebSockets (SignalR/Socket.io) for real-time status.
-   **Auth**: Bearer Token (JWT).
-   **Pagination**: All lists use `limit` (max 100) and `cursor` (opaque string). Returns `nextCursor` in metadata.

### 2.1. Implementation Guide: Opaque Cursors
To prevent "page skipping" issues common with offset pagination, we use keyset pagination.
**The "Opaque" String strategy:**
-   **Content**: A base64-encoded string combining the `SortKey` (e.g., CreatedAt) and the `UniqueId`.
-   **Format**: `Base64($"2024-01-01T12:00:00Z|{Guid}")`
-   **Server-Side Logic**:
    -   Decode the cursor.
    -   Query: `WHERE (CreatedAt < CursorDate) OR (CreatedAt == CursorDate AND Id < CursorId)`.
    -   Take: `limit`.
-   **Client-Side**: Treat the string as a black box. Just pass it back in the next request `?cursor=...`.

---

## 3. Endpoints

### 3.1. Deck Management

#### Data Model: `Deck`
| Field | Type | Required | Validation Rules | Description |
| :--- | :--- | :--- | :--- | :--- |
| `Id` | Guid | Yes | - | PK |
| `OwnerId` | Guid | Yes | - | FK to User |
| `Name` | String | Yes | Min 3, Max 100 chars | Unique per user |
| `Description` | String | No | Max 500 chars | - |
| `IsPublic` | Bool | Yes | - | - |
| `Settings` | JSON | Yes | - | See `DeckSettings` |

**DeckSettings Schema**:
```json
{
  "newCardsPerDay": { "min": 0, "max": 500, "default": 20 },
  "reviewLimitPerDay": { "min": 0, "max": 9999, "default": 200 },
  "newCardSortOrder": { "enum": ["random", "date_added", "alphabetical"], "default": "random" },
  "interdayLearningMix": { "type": "bool", "default": true, "description": "If true, short-interval learning cards are mixed with reviews." }
}
```

#### `GET /api/decks`
List all decks for the user.
Create a worker that sync the stats 
**Parameters**: `cursor`, `limit`.
**Response**:
```json
{
  "data": [
    {
      "id": "deck_guid",
      "name": "Advanced Biology",
      "description": "...",
      "stats": { "due": 12, "new": 5, "total": 150 }
    }
  ],
  "meta": { "nextCursor": "..." }
}
```

#### `POST /api/decks`
Create a new deck container.
**Request Body**:
```json
{
  "name": "Advanced Molecular Biology",
  "description": "Deep dive into cell structures.",
  "isPublic": true,
  "settings": { ... } // Optional: inherits from User Defaults if omitted
}
```
**Behind the Scenes**:
1.  **Validation**: `FluentValidation` for name/desc limits. Check DB for duplicate name for `OwnerId`.
2.  **Defaults**: If `settings` null, load `User.DefaultDeckSettings` from DB.
3.  **Search Indexing**: If `isPublic=true`, publish event `deck_created` to Search Indexer queue.
**Response (201 Created)**: Returns full Deck object.

#### `GET /api/decks/{id}`
Retrieve deck config, settings, and high-level engagement stats.
**Response**:
```json
{
  "id": "deck_guid",
  "name": "...",
  "subscriberCount": 142,
  "ownerId": "user_guid",
  "settings": { ... }
}
```

#### `DELETE /api/decks/{id}`
**Description**: Deletes a deck and all associated Notes and Cards.
**Behind the Scenes**:
1.  **Soft Delete**: Mark `IsDeleted = true`.
2.  **Background Cleanup**: Queue `cleanup_deck_data` to permanently remove thousands of cards/notes asynchronously to prevent DB timeouts.

#### `GET /api/decks/{id}/statistics`
**Description**: granular statistics for visualization.
**Response**:
```json
{
  "totalCards": 1500,
  "matureCards": 800, // Interval > 21 days
  "youngCards": 400,
  "learningCards": 100,
  "newCards": 200,
  "retentionRate": 0.92, // % of correct reviews
  "forecast": [ // For heatmaps/graphs
    { "date": "2024-01-01", "reps": 45 },
    { "date": "2024-01-02", "reps": 32 }
  ]
}
```

### 3.2. Note Type Management

#### Data Model: `NoteType`
| Field | Type | Required | Validation Rules | Description |
| :--- | :--- | :--- | :--- | :--- |
| `Id` | Guid | Yes | - | PK |
| `OwnerId` | Guid | Yes | - | FK (User or Null for System Types) |
| `Name` | String | Yes | Max 50 chars | Unique per user |
| `Fields` | JSON | Yes | Min 1 field | List of field definitions |
| `Templates` | JSON | Yes | Min 1 template | Card generation logic |
| `Css` | String | No | Max 10kb | Custom styling for cards |

**Fields Schema**: `[ { "name": "Front" }, { "name": "Back" } ]`
**Templates Schema**: `[ { "name": "Card 1", "front": "{{Front}}", "back": "{{Back}}" } ]`

#### `GET /api/notetypes`
List all note types available to the user (System defaults + Created by user).
**Parameters**: `cursor`, `limit`.
**Response**:
```json
{
  "data": [
    {
      "id": "guid_basic",
      "name": "Basic",
      "ownerId": null, // System type
      "fieldCount": 2,
      "templateCount": 1
    },
    {
      "id": "guid_custom_code",
      "name": "Programming Syntax",
      "ownerId": "user_guid", 
      ...
    }
  ]
}
```

#### `GET /api/notetypes/{id}`
Get full definition including fields, templates, and CSS.

#### `POST /api/notetypes`
**Description**: Define a new schema for generating cards.
**Request Body**:
```json
{
  "name": "Programming Syntax",
  "fields": [
    { "name": "Concept" },
    { "name": "CodeBlock" },
    { "name": "Explanation" }
  ],
  "templates": [
    {
      "name": "Explain Code",
      "front": "What does this code do?\n<pre>{{CodeBlock}}</pre>",
      "back": "{{Concept}}<br>{{Explanation}}"
    }
  ],
  "css": ".card { font-family: monospace; }"
}
```
**Behind the Scenes**:
1.  **Validation**: 
    -   Ensure all template variables (e.g., `{{CodeBlock}}`) exist in `fields`.
    -   Validate CSS (block external resources/scripts).
2.  **Safety**: Sanitize HTML in templates.

#### `PATCH /api/notetypes/{id}`
**Description**: Update fields or styling. **Owner Only**.
**Request**: Partial object (e.g., update `css` or add a `template`).
**Behind the Scenes**:
1.  **Auth Check**: `NoteType.OwnerId == CurrentUser.Id`.
2.  **Field Removal**: Any fields removed from the schema are permanently deleted from linked Notes. (UI responsibility to warn user).
3.  **Sync**: Triggers background job `regenerate_notetype_cards` (payload: `NoteTypeId`) to re-render all associated cards.

#### `DELETE /api/notetypes/{id}`
**Description**: Soft delete a note type. **Owner Only**.
**Rules**:
-   Cannot delete if Notes exist using this type (must delete Notes first).
-   Cannot delete System types.

#### `GET /api/notetypes/{id}/template`
**Description**: Get a blank template for importing data.
**Parameters**: `format=csv` (or json).
**Response (CSV)**: Returns a file with headers matching the fields (e.g., `Concept,CodeBlock,Explanation`).
**Response (JSON)**: `[ { "Concept": "", "CodeBlock": "", "Explanation": "" } ]`.

### 3.3. Note Management

#### Data Model: `Note`
| Field | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `Id` | Guid | Yes | PK |
| `DeckId` | Guid | Yes | FK to Deck |
| `NoteTypeId` | Guid | Yes | FK to NoteType |
| `Fields` | JSON | Yes | Key-Value pairs matching NoteType fields |
| `Tags` | List\<Str\> | No | Max 5 tags |
| `CreatedAt` | DateTime | Yes | - |
| `IsDeleted` | Bool | Yes | Soft delete flag |

#### `POST /api/decks/{id}/notes` (Bulk)
**Description**: The primary way to add content. Processing is asynchronous.
**Request Body**:
```json
{
  "notes": [
    {
      "noteTypeId": "guid_programming_syntax",
      "tags": ["python", "loops"],
      "fields": { "Concept": "For Loop", "CodeBlock": "for i in range(10):...", "Explanation": "Iterates..." }
    }
  ]
}
```
**Behind the Scenes**:
1.  **Validation**: 
    -   Check `Deck` exists and user is owner.
    -   Check `NoteType` exists.
    -   Validate `Fields` contains all required keys from NoteType.
2.  **Quota**: Check `User.DailyNoteLimit`.
3.  **Batch Insert**: Save `Notes` to DB with state `PendingGeneration`.
4.  **Queue**: Emit `generate_cards_batch` (Payload: List of NoteIds).
5.  **WebSocket**: Notify client of `job_started`.
**Response (202 Accepted)**: `{ "jobId": "job_gen_555" }`

#### `POST /api/decks/{id}/notes/generate` (AI)
**Description**: Generate notes using AI based on a topic and Note Type.
**Request Body**:
```json
{
  "noteTypeId": "guid_programming",
  "topic": "Python Lists",
  "additionalInfo": "Focus on list comprehension",
  "count": 10
}
```
**Behind the Scenes**:
1.  **Queue**: Emit `ai_generate_notes_command`.
2.  **Worker**: 
    -   Fetch NoteType fields.
    -   Prompt LLM: "Create 10 notes on 'Python Lists' (Context: 'Focus on list comprehension') matching schema: [Concept, CodeBlock, Explanation]".
    -   Parse JSON output -> Batch Insert Notes.
3.  **Notify**: `job_completed` with count.

#### `GET /api/decks/{id}/notes`
**Description**: List notes for table view/management.
**Parameters**: `cursor`, `limit`, `search` (full text search on Fields).
**Response**: Note objects with their `CardCounts` (generated count).

#### `PATCH /api/notes/{id}`
**Description**: Correction of typos or updating content.
**Request**: `{ "fields": { "Concept": "For Loop (Updated)" } }`
**Behind the Scenes**:
1.  **Update Note**: Save new field data.
2.  **Regenerate Cards**: Identify all Cards linked to this Note. Re-render their Front/Back HTML using the new field data.
3.  **Preserve State**: Do *not* reset SRS progress unless explicitly requested.

#### `DELETE /api/notes/{id}`
**Description**: Removes the note and cascades delete to all its generated cards.
**Behind the Scenes**:
1.  **Soft Delete**: `Note.IsDeleted = true`.
2.  **Cascade**: `Cards.IsDeleted = true` (via trigger or app logic).
3.  **Queue**: `cleanup_deck_data` (optional immediate trigger or scheduled).

### 3.4. Card Management

#### Data Model: `Card`
| Field | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `Id` | Guid | Yes | PK |
| `NoteId` | Guid | Yes | FK to Note |
| `Front` | String | Yes | Rendered HTML |
| `Back` | String | Yes | Rendered HTML |
| `State` | Enum | Yes | New/Learning/Review/Relearning |
| `DueDate` | DateTime | Yes | SRS scheduling |
| `Interval` | Int | Yes | Days |
| `Ease` | Float | Yes | Multiplier (default 2.5) |

#### `GET /api/decks/{id}/cards`
**Description**: Browser view for managing cards.
**Parameters**: `cursor`, `limit`, `sort` (created_at, due_date), `search` (content query).

#### `DELETE /api/cards/{id}`
**Status**: 405 Method Not Allowed.
**Reasoning**: Cards are views of Notes. To delete a card, you must edit the Note (changing templates) or delete the Note itself.

#### `PATCH /api/cards/{id}`
**Description**: Manage SRS state manually or Flag/Bury cards.
**Request**:
```json
{
  "action": "bury", // "suspend", "reset", "bury"
  "date": "2024-01-02" // Optional override
}
```
**Behind the Scenes**:
-   **Bury**: Push `DueDate` to tomorrow + 1 day.
-   **Suspend**: Set `IsSuspended = true` (exclude from study).
-   **Reset**: Set `Interval=0`, `Ease=2.5`, `State=New`.

### 3.5. Import & Export

#### `POST /api/decks/{id}/import/text`
**Description**: Upload a CSV or TSV file.
**Request**: generic `multipart/form-data` file upload.
**Headers**: `X-Delimiter: \t`, `X-Note-Type-Id: ...`.
**Behind the Scenes**:
1.  **Stream Read**: Parse file line-by-line using limit/offset to handle large files.
2.  **Map Fields**: Map column 0 to Field 1, column 1 to Field 2, etc.
3.  **Batch Create**: Create Notes in chunks of 100.
4.  **Job Tracking**: Return `jobId` for progress bar.

### 3.6. Study Session (The Engine)

#### `GET /api/decks/{id}/study/next`
**Description**: Smart selection of the single most important card to review *right now*.
**Algorithm**:
1.  **Fetch Config**: Load Deck options (limits/mix).
2.  **Review Check**:
    -   Query: `WHERE DueDate <= Now AND State = Review`.
    -   Sort: `DueDate ASC` (Standard) or `Random`.
    -   Limit: 1.
3.  **Learning Check** (if Reviews empty or mixed):
    -   Query: `WHERE State = Learning AND NextStep <= Now`.
4.  **New Card Check** (if others empty):
    -   Check `DailyNewCardCount` < `Limit`.
    -   Query: `WHERE State = New`.
    -   Sort: `Order ASC` (User defined: random/alpha).
5.  **Return**: Rendered HTML Front, CardID, NoteID.

#### `POST /api/reviews/{cardId}` (Blocking)
**Request**: `{ "userAnswer": "The answer...", "timeTakenMs": 5000 }`
**Behind the Scenes**:
1.  **Rate Limit**: Ensure user valid.
2.  **Queue**: `grade_answer_queue`.
3.  **Log**: Create `ReviewLog` (Status: Processing).
**Response (202)**: `{ "reviewId": "...", "pollUrl": "..." }`

### 3.7. Job System & WebSockets

#### Connection & Auth
-   **Endpoint**: `/ws/hub` (SignalR)
-   **Auth**: Bearer Token passed via Query String `?access_token=...` (Standard SignalR pattern).
-   **Transport**: WebSockets primary, fallback to Server-Sent Events / Long Polling.

#### API: Cancel Job
**`POST /api/jobs/{id}/cancel`**
-   **Logic**: Sets Redis key `cancellation_token:{jobId}` to `true`. Workers check this key periodically.

#### Event Reference
All events are sent to the specific user's group (`User:{UserId}`).

| Event Name | Payload Structure | Description |
| :--- | :--- | :--- |
| `job_started` | `{ jobId, type }` | Acknowledgment that a background task began. |
| `job_progress` | `{ jobId, percent, message }` | Progress update (e.g., "Importing 50/100"). |
| `job_completed` | `{ jobId, result }` | Task finished successfully. |
| `job_failed` | `{ jobId, error }` | Task failed with reason. |
| `review_graded` | `{ reviewId, cardId, score }` | AI has finished grading your answer. |

## 4. Background Workers (Detailed)

### 4.1. Card Generator Worker
**Trigger**: `generate_cards_batch`
**Process**:
1.  **Load Batch**: Fetch 50 Notes.
2.  **Load Schema**: Fetch NoteType & Templates.
3.  **Loop**:
    -   Parse Front/Back templates.
    -   **Regex Replace**: Replace `{{FieldName}}` with `Note.Fields["FieldName"]`.
    -   **Cloze logic** (if needed): Detect `{{c(\d+)::(.*?)}}`. Create Card for each index `\d+`. Substitution: `[...]` for active cloze, text for inactive.
4.  **Dedupe**: Check `Cards` table for `hash(FrontContent)` to avoid duplicates.
5.  **Insert**: Bulk insert new Cards.
6.  **Progress**: `PublishProgress(current/total)`.

### 4.2. Cleanup Data Worker
**Trigger**: `cleanup_deck_data` or Scheduled Cron (e.g., Daily).
**Purpose**: Permanently remove soft-deleted entities after a grace period (e.g., 30 days) to reclaim storage.
**Process**:
1.  **Query**: `SELECT Id FROM Decks/Notes/Cards WHERE IsDeleted = 1 AND DeletedAt < (Now - 30Days)`.
2.  **Batch Delete**: Remove records in chunks of 1000 to prevent locking.
3.  **Media Cleanup**: Check for orphaned images/audio in Object Storage and delete them.

### 4.3. AI Grading Worker
**Trigger**: `grade_answer_queue`
**Process**:
1.  **Context Assembly**:
    -   Card Front (Question)
    -   Card Back (Ideal Answer)
    -   User Answer
    -   Previous Review History (optional, to see if they make the same mistake).
2.  **LLM Call**:
    -   *Model*: Low-latency model (e.g., GPT-3.5-Turbo, Gemini Flash).
    -   *Prompt*: "Compare user answer with ideal answer. Be lenient on typos. Strict on concept. Output JSON: { score_0_to_5, brief_feedback }."
3.  **SRS Update**:
    -   Calculate `NewInterval`, `Ease`, `DueDate` (SM-2 Modified).
    -   *Fail (0-2)*: Interval -> 1min, 10min (Learning steps).
    -   *Pass (3-5)*: Geometric growth.
4.  **DB Update**: Atomic update of `UserCard` and `ReviewLog`.
5.  **Push**: Send `review.graded` via SignalR/WebSocket.

### 4.4. Search Indexer Worker
**Trigger**: `deck_created`, `deck_updated`.
**Process**:
1.  **Update Index**: Updates Elasticsearch/Lucene index with Deck Name, Description, and Tags.
2.  **Optimization**: Delays commits (debounce) to avoid thrashing on bulk updates.

### 4.5. NoteType Regenerator Worker
**Trigger**: `regenerate_notetype_cards`.
**Process**:
1.  **Fetch**: Get all Notes using the modified `NoteType`.
2.  **Re-render**: Loop through notes, apply new Templates/CSS.
3.  **Update**: Bulk update `Cards` table with new `Front/Back` HTML.

### 4.6. Import Processor Worker
**Trigger**: `import_text_batch`.
**Process**:
1.  **Stream**: Read file from Object Storage.
2.  **Parse**: Iterate lines, split by delimiter.
3.  **Map**: Create `Note` objects based on mapping config.
4.  **Batch Insert**: Writes in transactions of 500.
5.  **Check Cancel**: Checks Redis cancellation token every batch.

## 5. Security & Validation
-   **XSS Protection**: While we allow HTML in cards, strict sanitization (DOMPurify) is applied before rendering on client.
-   **Resource Quotas**: Max 5000 new cards/day per user. Max file upload size 10MB.

### 6. Upcoming / TODO
-   `PATCH /api/users/me/settings`: Global user preferences (including default deck settings, username, theme).
-   **Refactor Tags**: Convert Tags from simple strings to a first-class `Tag` entity (Id, Name, Count) for better management and merging.

