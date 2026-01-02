# Pulse Surveys Feature - Backup Documentation

This document contains all the code needed to recreate the Pulse Surveys web frontend and Supabase edge function.
These were on the remote branch but lost during git cleanup on January 2, 2026.

## Overview

The Pulse Surveys feature consists of:
1. **Static Web App** (`tracker-surveys/`) - HTML/CSS/JS site hosted on Cloudflare Workers
2. **Supabase Edge Function** (`Tracker/supabase/functions/tracker-survey/`) - API endpoint
3. **Supabase Config** - Function configuration in `config.toml`
4. **Seed Data** - Test survey data in `seed.sql`

The flow:
1. Manager creates a survey in Tracker desktop app
2. Tracker generates unique tokens for each team member
3. Team members receive a link like `https://your-site.workers.dev?token=ABC123`
4. Static site calls Supabase edge function to load/submit survey
5. Responses are stored in Supabase and synced back to Tracker

---

## File 1: `tracker-surveys/index.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Tracker Survey</title>
  <link rel="stylesheet" href="styles.css">
</head>
<body>
  <div class="container">
    <div id="loading" class="loading">
      <div class="spinner"></div>
      <p>Loading survey...</p>
    </div>

    <div id="error" class="error-container" style="display: none;">
      <div class="error-icon"></div>
      <h1 id="error-title">Error</h1>
      <p id="error-message"></p>
    </div>

    <div id="survey" style="display: none;">
      <div class="header">
        <div class="logo"> Tracker</div>
        <h1 id="survey-title"></h1>
        <p id="survey-description" class="description"></p>
      </div>

      <form id="survey-form" class="survey-form">
        <div id="questions"></div>
        <button type="submit" class="submit-btn">Submit Response</button>
      </form>

      <p class="privacy-note"> Your response is confidential.</p>
    </div>

    <div id="thankyou" class="thankyou-container" style="display: none;">
      <div class="checkmark"></div>
      <h1>Thank You!</h1>
      <p>Your response to <span id="thankyou-title" class="survey-title"></span> has been recorded.</p>
      <p style="margin-top: 20px; font-size: 14px;">You can close this window now.</p>
    </div>
  </div>

  <script src="survey.js"></script>
</body>
</html>
```

---

## File 2: `tracker-surveys/styles.css`

```css
/* Base styles */
* { box-sizing: border-box; margin: 0; padding: 0; }

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
  min-height: 100vh;
  padding: 20px;
  color: #e0e0e0;
}

.container {
  max-width: 600px;
  margin: 0 auto;
}

/* Loading */
.loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 50vh;
}

.spinner {
  width: 50px;
  height: 50px;
  border: 4px solid rgba(0, 217, 255, 0.2);
  border-top-color: #00d9ff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Error */
.error-container, .thankyou-container {
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 50vh;
}

.error-icon {
  width: 80px;
  height: 80px;
  background: rgba(255, 107, 107, 0.2);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 30px;
  font-size: 40px;
}

.error-container h1 {
  font-size: 28px;
  color: #ff6b6b;
  margin-bottom: 16px;
}

/* Thank you */
.checkmark {
  width: 80px;
  height: 80px;
  background: linear-gradient(135deg, #00d9ff 0%, #00a8cc 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 30px;
  font-size: 40px;
  color: #1a1a2e;
}

.thankyou-container h1 {
  font-size: 32px;
  color: #ffffff;
  margin-bottom: 16px;
}

.survey-title {
  color: #00d9ff;
  font-weight: 600;
}

/* Survey header */
.header {
  text-align: center;
  margin-bottom: 30px;
}

.logo {
  font-size: 24px;
  font-weight: bold;
  color: #00d9ff;
  margin-bottom: 10px;
}

h1 {
  font-size: 28px;
  color: #ffffff;
  margin-bottom: 10px;
}

.description {
  color: #a0a0a0;
  font-size: 16px;
}

/* Survey form */
.survey-form {
  background: rgba(255, 255, 255, 0.05);
  border-radius: 16px;
  padding: 30px;
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.question {
  margin-bottom: 30px;
  padding-bottom: 30px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.question:last-child {
  border-bottom: none;
  margin-bottom: 20px;
}

.question-number {
  font-size: 12px;
  color: #00d9ff;
  text-transform: uppercase;
  letter-spacing: 1px;
  margin-bottom: 8px;
}

.question-text {
  font-size: 18px;
  color: #ffffff;
  margin-bottom: 16px;
  line-height: 1.4;
}

.required { color: #ff6b6b; }

/* Rating styles */
.rating-group {
  display: flex;
  gap: 10px;
  justify-content: center;
  margin-bottom: 10px;
}

.rating-option { cursor: pointer; }
.rating-option input { display: none; }

.rating-circle {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.1);
  color: #a0a0a0;
  font-size: 18px;
  font-weight: 600;
  transition: all 0.2s;
  border: 2px solid transparent;
}

.rating-option:hover .rating-circle {
  background: rgba(0, 217, 255, 0.2);
  color: #00d9ff;
}

.rating-option input:checked + .rating-circle {
  background: #00d9ff;
  color: #1a1a2e;
  border-color: #00d9ff;
}

.rating-labels {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: #666;
  padding: 0 10px;
}

/* Choice styles */
.choice-group {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.choice-group.horizontal {
  flex-direction: row;
  gap: 20px;
}

.choice-option {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 18px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s;
  border: 2px solid transparent;
}

.choice-option:hover {
  background: rgba(0, 217, 255, 0.1);
}

.choice-option input { display: none; }

.choice-option input:checked + span {
  color: #00d9ff;
}

.choice-option:has(input:checked) {
  border-color: #00d9ff;
  background: rgba(0, 217, 255, 0.15);
}

/* Text input styles */
textarea, input[type="text"] {
  width: 100%;
  padding: 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 2px solid rgba(255, 255, 255, 0.1);
  border-radius: 10px;
  color: #ffffff;
  font-size: 16px;
  font-family: inherit;
  resize: vertical;
  transition: border-color 0.2s;
}

textarea:focus, input[type="text"]:focus {
  outline: none;
  border-color: #00d9ff;
}

textarea::placeholder { color: #666; }

/* Submit button */
.submit-btn {
  width: 100%;
  padding: 16px 32px;
  background: linear-gradient(135deg, #00d9ff 0%, #00a8cc 100%);
  color: #1a1a2e;
  border: none;
  border-radius: 10px;
  font-size: 18px;
  font-weight: 600;
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s;
}

.submit-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(0, 217, 255, 0.3);
}

.submit-btn:active { transform: translateY(0); }

.submit-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.privacy-note {
  text-align: center;
  margin-top: 20px;
  font-size: 13px;
  color: #666;
}

@media (max-width: 480px) {
  body { padding: 10px; }
  .survey-form { padding: 20px; }
  h1 { font-size: 24px; }
  .rating-circle { width: 40px; height: 40px; font-size: 16px; }
}
```

---

## File 3: `tracker-surveys/survey.js`

```javascript
// Tracker Survey - Static Site JavaScript
const API_BASE = 'https://cftzoxucrzqljadyiijd.supabase.co/functions/v1/tracker-survey';

// Get token from URL
const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get('token');

// DOM elements
const loadingEl = document.getElementById('loading');
const errorEl = document.getElementById('error');
const surveyEl = document.getElementById('survey');
const thankyouEl = document.getElementById('thankyou');

function showError(title, message) {
  loadingEl.style.display = 'none';
  surveyEl.style.display = 'none';
  thankyouEl.style.display = 'none';
  document.getElementById('error-title').textContent = title;
  document.getElementById('error-message').textContent = message;
  errorEl.style.display = 'flex';
}

function showThankYou(surveyTitle) {
  loadingEl.style.display = 'none';
  surveyEl.style.display = 'none';
  errorEl.style.display = 'none';
  document.getElementById('thankyou-title').textContent = surveyTitle;
  thankyouEl.style.display = 'flex';
}

function renderQuestion(question, index) {
  const div = document.createElement('div');
  div.className = 'question';

  let inputHtml = '';
  const required = question.is_required ? 'required' : '';
  const requiredMark = question.is_required ? ' <span class="required">*</span>' : '';

  switch (question.question_type) {
    case 'rating':
      const maxRating = question.options?.maxRating || 5;
      const lowLabel = question.options?.lowLabel || 'Low';
      const highLabel = question.options?.highLabel || 'High';
      inputHtml = '<div class="rating-group">' +
        Array.from({length: maxRating}, (_, i) => i + 1)
          .map(num => '<label class="rating-option"><input type="radio" name="q_' + question.id + '" value="' + num + '" required><span class="rating-circle">' + num + '</span></label>')
          .join('') +
        '</div><div class="rating-labels"><span>' + lowLabel + '</span><span>' + highLabel + '</span></div>';
      break;

    case 'text':
      inputHtml = '<textarea name="q_' + question.id + '" rows="3" ' + required + ' placeholder="Enter your response..."></textarea>';
      break;

    case 'multiple_choice':
      const choices = question.options?.choices || [];
      inputHtml = '<div class="choice-group">' +
        choices.map(choice => '<label class="choice-option"><input type="radio" name="q_' + question.id + '" value="' + choice + '" ' + required + '><span>' + choice + '</span></label>').join('') +
        '</div>';
      break;

    case 'yes_no':
      inputHtml = '<div class="choice-group horizontal">' +
        '<label class="choice-option"><input type="radio" name="q_' + question.id + '" value="Yes" ' + required + '><span>Yes</span></label>' +
        '<label class="choice-option"><input type="radio" name="q_' + question.id + '" value="No" ' + required + '><span>No</span></label>' +
        '</div>';
      break;

    default:
      inputHtml = '<input type="text" name="q_' + question.id + '" ' + required + '>';
  }

  div.innerHTML = '<div class="question-number">Question ' + (index + 1) + '</div>' +
    '<div class="question-text">' + question.question_text + requiredMark + '</div>' +
    inputHtml;

  return div;
}

async function loadSurvey() {
  if (!token) {
    showError('Invalid Link', 'Please use the survey link provided by your manager.');
    return;
  }

  try {
    const response = await fetch(API_BASE + '?token=' + encodeURIComponent(token));
    const data = await response.json();

    if (data.error) {
      showError(data.error, data.message || '');
      return;
    }

    // Render survey
    document.getElementById('survey-title').textContent = data.survey.title;
    document.getElementById('survey-description').textContent = data.survey.description || '';

    const questionsContainer = document.getElementById('questions');
    data.questions.forEach((q, i) => {
      questionsContainer.appendChild(renderQuestion(q, i));
    });

    // Show survey
    loadingEl.style.display = 'none';
    surveyEl.style.display = 'block';

    // Store survey title for thank you page
    surveyEl.dataset.title = data.survey.title;

  } catch (err) {
    console.error('Load error:', err);
    showError('Connection Error', 'Unable to load survey. Please try again later.');
  }
}

async function submitSurvey(event) {
  event.preventDefault();

  const form = event.target;
  const submitBtn = form.querySelector('.submit-btn');
  submitBtn.disabled = true;
  submitBtn.textContent = 'Submitting...';

  const formData = new FormData(form);
  const answers = [];

  for (const [key, value] of formData.entries()) {
    if (key.startsWith('q_')) {
      answers.push({
        question_id: key.replace('q_', ''),
        answer_value: value
      });
    }
  }

  try {
    const response = await fetch(API_BASE + '?token=' + encodeURIComponent(token), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ answers })
    });

    const data = await response.json();

    if (data.error) {
      showError('Submission Failed', data.message || 'Please try again.');
      return;
    }

    showThankYou(surveyEl.dataset.title);

  } catch (err) {
    console.error('Submit error:', err);
    submitBtn.disabled = false;
    submitBtn.textContent = 'Submit Response';
    showError('Connection Error', 'Unable to submit. Please try again.');
  }
}

// Initialize
document.getElementById('survey-form').addEventListener('submit', submitSurvey);
loadSurvey();
```

---

## File 4: `Tracker/supabase/functions/tracker-survey/index.ts`

```typescript
// Tracker Survey Edge Function - JSON API
// Serves survey data as JSON for the static site to render

import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

Deno.serve(async (req: Request) => {
  // Handle CORS preflight
  if (req.method === "OPTIONS") {
    return new Response(null, { status: 204, headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);
    const token = url.searchParams.get("token");

    // Initialize Supabase client
    const supabaseUrl = Deno.env.get("SUPABASE_URL");
    const supabaseKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");

    if (!supabaseUrl || !supabaseKey) {
      return jsonResponse({ error: "Configuration Error", message: "Service not configured" }, 500);
    }

    const supabase = createClient(supabaseUrl, supabaseKey);

    if (!token) {
      return jsonResponse({ error: "Invalid Link", message: "Please use the survey link provided by your manager." }, 400);
    }

    // GET = Return survey data as JSON
    if (req.method === "GET") {
      return await getSurveyData(supabase, token);
    }

    // POST = Submit survey response
    if (req.method === "POST") {
      return await submitSurveyResponse(supabase, token, req);
    }

    return jsonResponse({ error: "Method Not Allowed" }, 405);
  } catch (error) {
    console.error("Unhandled error:", error);
    return jsonResponse({ error: "Server Error", message: error instanceof Error ? error.message : "Unknown error" }, 500);
  }
});

function jsonResponse(data: any, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

async function getSurveyData(supabase: any, token: string): Promise<Response> {
  // Validate token and get survey
  const { data: tokenData, error: tokenError } = await supabase
    .from("survey_tokens")
    .select("*, survey:surveys(*, questions:survey_questions(*))")
    .eq("token", token)
    .single();

  if (tokenError || !tokenData) {
    return jsonResponse({ error: "Survey Not Found", message: "This survey link is invalid or has expired." }, 404);
  }

  if (tokenData.used_at) {
    return jsonResponse({ error: "Already Completed", message: "You have already submitted your response. Thank you!" }, 400);
  }

  if (tokenData.expires_at && new Date(tokenData.expires_at) < new Date()) {
    return jsonResponse({ error: "Link Expired", message: "This survey link has expired. Please contact your manager for a new link." }, 400);
  }

  const survey = tokenData.survey;
  const questions = survey.questions.sort((a: any, b: any) => a.sort_order - b.sort_order);

  return jsonResponse({
    survey: {
      id: survey.id,
      title: survey.title,
      description: survey.description,
      is_anonymous: survey.is_anonymous,
    },
    questions: questions.map((q: any) => ({
      id: q.id,
      question_text: q.question_text,
      question_type: q.question_type,
      is_required: q.is_required,
      options: q.options,
    })),
  });
}

async function submitSurveyResponse(supabase: any, token: string, req: Request): Promise<Response> {
  // Validate token
  const { data: tokenData, error: tokenError } = await supabase
    .from("survey_tokens")
    .select("*, survey:surveys(*)")
    .eq("token", token)
    .single();

  if (tokenError || !tokenData) {
    return jsonResponse({ error: "Invalid Token", message: "This survey link is not valid." }, 400);
  }

  if (tokenData.used_at) {
    return jsonResponse({ error: "Already Submitted", message: "This survey has already been completed." }, 400);
  }

  // Parse JSON body
  const body = await req.json();
  const answers = body.answers || [];

  // Create response record (using actual column names)
  const { data: response, error: responseError } = await supabase
    .from("survey_responses")
    .insert({
      survey_id: tokenData.survey_id,
      token_id: tokenData.id,
      respondent_name: tokenData.team_member_name,
      submitted_at: new Date().toISOString(),
    })
    .select()
    .single();

  if (responseError) {
    console.error("Error creating response:", responseError);
    return jsonResponse({ error: "Save Failed", message: "We couldn't save your response. Please try again." }, 500);
  }

  // Get questions to determine answer types
  const { data: questions } = await supabase
    .from("survey_questions")
    .select("id, question_type")
    .eq("survey_id", tokenData.survey_id);

  const questionTypes = new Map(questions?.map((q: any) => [q.id, q.question_type]) || []);

  // Insert answers with proper columns based on question type
  const answerRecords = answers.map((a: any) => {
    const qType = questionTypes.get(a.question_id);
    const record: any = {
      response_id: response.id,
      question_id: a.question_id,
    };

    // Map answer to correct column based on question type
    if (qType === "rating") {
      record.answer_rating = parseInt(a.answer_value, 10);
    } else if (qType === "yes_no") {
      record.answer_boolean = a.answer_value === "Yes";
    } else {
      record.answer_text = a.answer_value;
    }

    return record;
  });

  const { error: answersError } = await supabase.from("survey_answers").insert(answerRecords);

  if (answersError) {
    console.error("Error saving answers:", answersError);
  }

  // Mark token as used
  await supabase
    .from("survey_tokens")
    .update({ used_at: new Date().toISOString() })
    .eq("id", tokenData.id);

  return jsonResponse({ success: true, message: "Response recorded" });
}
```

---

## File 5: `Tracker/supabase/config.toml` (add to existing)

```toml
[functions.tracker-survey]
verify_jwt = false
```

---

## File 6: `Tracker/supabase/seed.sql`

```sql
-- Tracker Seed Data
-- Run this to populate test data after schema setup
-- Usage: psql -f seed.sql or run via Supabase SQL Editor

-- ============================================
-- TEST SURVEY DATA
-- ============================================

-- Create a test survey
INSERT INTO surveys (id, title, description, is_anonymous, is_active, created_at)
VALUES (
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  'Q4 2025 Team Pulse Check',
  'Quick check-in on how the team is feeling about current projects and workload.',
  false,
  true,
  NOW()
) ON CONFLICT (id) DO NOTHING;

-- Create survey questions
INSERT INTO survey_questions (id, survey_id, question_text, question_type, options, is_required, order_index)
VALUES
  -- Rating question
  (
    'q1111111-1111-1111-1111-111111111111',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'How would you rate your current workload?',
    'rating',
    '{"maxRating": 5, "lowLabel": "Too Light", "highLabel": "Overwhelming"}',
    true,
    1
  ),
  -- Multiple choice question
  (
    'q2222222-2222-2222-2222-222222222222',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'Which area needs the most improvement?',
    'multiple_choice',
    '{"choices": ["Communication", "Tools & Resources", "Work-Life Balance", "Career Growth", "Team Collaboration"]}',
    true,
    2
  ),
  -- Yes/No question
  (
    'q3333333-3333-3333-3333-333333333333',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'Do you feel supported by your manager?',
    'yes_no',
    '{}',
    true,
    3
  ),
  -- Text question
  (
    'q4444444-4444-4444-4444-444444444444',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'What''s one thing we could do to improve your work experience?',
    'text',
    '{}',
    false,
    4
  )
ON CONFLICT (id) DO NOTHING;

-- Create a test token (valid for 7 days)
INSERT INTO survey_tokens (id, survey_id, token, team_member_id, team_member_name, expires_at, created_at)
VALUES (
  't0000000-0000-0000-0000-000000000001',
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  'TEST-TOKEN-12345',
  NULL,
  'Test User',
  NOW() + INTERVAL '7 days',
  NOW()
) ON CONFLICT (id) DO NOTHING;

-- Output the test URL
SELECT 'Test Survey URL: https://your-site.workers.dev?token=TEST-TOKEN-12345' AS info;
```

---

## Database Tables Required

The survey feature requires these Supabase tables (should already exist):
- `surveys` - Survey definitions
- `survey_questions` - Questions for each survey
- `survey_tokens` - Unique tokens per respondent
- `survey_responses` - Submitted responses
- `survey_answers` - Individual answers

---

## Deployment Notes

### Static Site (Cloudflare Workers / Pages)
1. Create `tracker-surveys/` folder with the 3 files above
2. Deploy to Cloudflare Pages or any static host
3. Update `API_BASE` in `survey.js` to your Supabase function URL

### Supabase Edge Function
1. Create `Tracker/supabase/functions/tracker-survey/index.ts`
2. Add config to `config.toml`
3. Deploy: `supabase functions deploy tracker-survey`

### Configuration
- The function needs `SUPABASE_URL` and `SUPABASE_SERVICE_ROLE_KEY` env vars (auto-provided by Supabase)
- `verify_jwt = false` allows anonymous access (required for public survey links)
