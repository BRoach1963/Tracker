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
