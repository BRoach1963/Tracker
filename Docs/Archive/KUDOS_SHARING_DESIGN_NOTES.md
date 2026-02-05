# Kudos Sharing Architecture - Needs Development

**Status**: Design issue identified, needs rework before implementation

## Problem

Current design assumes single pre-configured channel for all kudos sharing (e.g., one Slack channel, one Teams webhook). This is too limiting for real-world usage.

## Real-World Use Cases

1. **Team-specific kudos** → Share to #engineering-team, #sales-team, etc.
2. **Company-wide recognition** → Share to org-wide #recognition channel
3. **Private kudos** → Send as DM or keep in ProCohere only
4. **Project-specific kudos** → Share to #project-apollo, #customer-success, etc.
5. **Department kudos** → Share to specific department channels
6. **1:1 kudos** → Send as direct message in Slack/Teams

## Proposed Solutions

### Option 1: Per-Kudos Channel Selection (Recommended)
- When creating/viewing kudos in ProCohere, show "Share to..." button
- Opens dropdown with available Slack channels (fetched from Slack API)
- User selects destination at share time
- No pre-configuration needed
- Most flexible approach

### Option 2: Hybrid Approach
- Optional org setting: "Auto-post PUBLIC kudos to #recognition"
- All other kudos have manual "Share" button with channel picker
- Balances automation with flexibility

### Option 3: Context-Aware Suggestions
- Suggest channels based on team membership
- If kudos involves Engineering team members → suggest #engineering
- If marked "public" → suggest org-wide #recognition
- Always allow manual override

## Implementation Requirements

### Database Changes
- Add `shared_to_slack_channel` column to kudos table (nullable, stores channel ID if shared)
- Add `shared_to_teams_webhook` column (nullable, stores which webhook was used)
- Add timestamps: `shared_at`, track when/where kudos was posted externally

### Slack Integration Needs
- Fetch list of channels user has access to (via `conversations.list` API)
- Post message to selected channel (via `chat.postMessage` API)
- Handle DMs differently (via `conversations.open` + `chat.postMessage`)

### Teams Integration Needs
- Similar approach - get list of available channels
- Post to selected channel or webhook

### UI Requirements
- Kudos detail view: "Share to Slack" button
- Channel picker dialog with search/filter
- Show sharing history ("Shared to #engineering on 2/5/26")
- Option to share multiple times to different channels

## Next Steps

1. Hold off on implementing kudos sharing until this design is finalized
2. Focus on core kudos functionality first (create, view, database storage)
3. Add sharing as Phase 2 feature with proper channel selection UI
4. Consider whether org-level default channel makes sense as optional setting

## Related Files
- `Models/Kudos.cs` - Will need additional columns for tracking shares
- `Services/*` - Future SlackService, TeamsService for posting
- `org_settings.settings_json` - May store optional default channels, but shouldn't be required

---

**Documented**: February 5, 2026  
**Identified by**: User feedback during appsettings.json review
