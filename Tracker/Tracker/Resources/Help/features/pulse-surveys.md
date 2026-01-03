# Pulse Surveys

Pulse Surveys are **quick check-ins** that help you measure team engagement, satisfaction, and sentiment without the overhead of lengthy annual surveys. Tracker's Pulse Surveys feature includes the ability to send surveys via external links—perfect for team members who don't have direct access to the Tracker application.

## Why Pulse Surveys Matter

Regular pulse checks help you:
- Identify issues before they escalate
- Track team morale and engagement trends
- Gather honest feedback (especially with anonymous surveys)
- Make data-driven decisions about team health
- Engage remote or external team members via shareable links

**Research shows** that frequent, short surveys yield higher response rates and more actionable insights than annual engagement surveys.

## Creating a Pulse Survey

### Basic Setup
1. Navigate to **Circle** → **Pulse Surveys**
2. Click **+ New Survey**
3. Enter a title (e.g., "Weekly Check-In", "Q4 Engagement Pulse")
4. Add a description with instructions for respondents
5. Add questions
6. Save

### Survey Properties

| Field | Description |
|-------|-------------|
| **Title** | The survey name shown to respondents |
| **Description** | Instructions or context for the survey |
| **Anonymous** | Whether responses hide the respondent's identity |
| **Due Date** | Optional deadline for responses |
| **Status** | Draft, Active, Closed, or Archived |

### Survey Statuses

| Status | Description |
|--------|-------------|
| **Draft** | Survey is being created/edited—not yet sent |
| **Active** | Survey is live and accepting responses |
| **Closed** | Survey is no longer accepting responses |
| **Archived** | Historical survey kept for records |

## Adding Questions

### Question Types

Tracker supports multiple question formats:

| Type | Description | Use When |
|------|-------------|----------|
| **Rating** | 1-5 scale (Strongly Disagree to Strongly Agree) | Measuring sentiment, satisfaction, agreement |
| **Text** | Open-ended text response | Gathering detailed feedback, suggestions |
| **Yes/No** | Binary choice | Simple yes/no questions |
| **Multiple Choice** | Select from predefined options | Categorical questions with fixed answers |

### Adding a Question
1. Click **+ Add** in the Questions section
2. Enter the question text
3. Select the question type
4. Mark as required if necessary
5. For Multiple Choice, add the available options

### Question Order
Questions are automatically numbered. Drag and drop to reorder (or edit sort order values).

## External Survey Links

One of Tracker's most powerful features is the ability to **share surveys via external links**. This allows team members who don't have Tracker installed to complete surveys from any device with a web browser.

### How External Links Work

```
┌────────────────┐     ┌──────────────────┐     ┌────────────────┐
│   Manager in   │────▶│  Generate Token  │────▶│  Survey Link   │
│    Tracker     │     │  for each person │     │   + QR Code    │
└────────────────┘     └──────────────────┘     └────────────────┘
                                                        │
                                                        ▼
┌────────────────┐     ┌──────────────────┐     ┌────────────────┐
│   Responses    │◀────│   Team Member    │◀────│  External Web  │
│  Sync to App   │     │    Submits       │     │     Form       │
└────────────────┘     └──────────────────┘     └────────────────┘
```

### Generating Survey Links

1. Select an **Active** survey
2. Click **Generate Links**
3. Choose which team members should receive the survey
4. Click **Generate**
5. Copy links or QR codes for each recipient

Each team member gets a **unique token** that:
- Can only be used once
- Links their response to them (or stays anonymous if survey is anonymous)
- Has an optional expiration date

### Survey Link Format
```
https://polished-wood-b404.brian-6df.workers.dev?token=UNIQUE-TOKEN
```

### Sharing Options
- **Copy Link**: Copy the URL to share via email, Slack, Teams, etc.
- **QR Code**: Generate a QR code for in-person distribution

### External Survey Experience

When team members open the link, they see:
1. Survey title and description
2. Questions in order
3. Submit button

After submitting:
- "Thank You" confirmation message
- Token is marked as used (prevents re-submission)
- Response syncs back to Tracker

## Viewing Results

### Response Summary
The Results view shows:
- Total responses received
- Response rate (if sent to specific team members)
- Average ratings for rating questions
- Individual text responses

### Syncing External Responses

External survey responses are stored in the cloud and sync to your local Tracker app:

1. Responses are saved immediately when submitted
2. Click **Sync Responses** to pull the latest data
3. New responses appear in the Results view

**Note**: If you're offline, responses will sync when you reconnect.

### Analyzing Feedback

For rating questions, Tracker shows:
- Average score
- Distribution chart (how many picked each rating)
- Trend over time (if you run recurring pulse surveys)

For text questions:
- Full responses are listed
- Anonymous surveys show responses without names

## Best Practices

### Survey Design
- **Keep it short**: 3-7 questions is ideal for pulse surveys
- **Mix question types**: Use ratings for metrics, text for insights
- **Be specific**: "How satisfied are you with team communication?" beats "How are things?"
- **Include one open question**: "What's one thing we could improve?" yields actionable feedback

### Frequency
- **Weekly**: Quick 2-3 question check-ins
- **Bi-weekly**: Standard pulse surveys (5-7 questions)
- **Monthly**: More comprehensive engagement checks

### Acting on Feedback
- **Acknowledge**: Let the team know you read their feedback
- **Act**: Make at least one visible change based on feedback
- **Follow up**: Ask about the change in the next pulse

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New Survey |
| `Ctrl+S` | Save Changes |
| `Escape` | Cancel Editing |

## Troubleshooting

### Survey link says "Invalid"
- The token may have expired
- Generate a new link for the team member

### Survey link says "Already Completed"
- Each link can only be used once
- Check if the response already appears in Results

### Responses not appearing
- Click **Sync Responses** to fetch the latest
- Check your internet connection
- External responses require cloud sync

### Team member can't access survey
- Ensure they have internet access
- Try generating a new link
- Check if the survey is still Active (not Closed)

## Related Topics
- [Team Members](team-members.md) - Managing your team
- [Feedback](feedback.md) - Recording individual feedback
- [1:1 Meetings](one-on-ones.md) - Discussing survey results
