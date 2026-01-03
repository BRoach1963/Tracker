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
