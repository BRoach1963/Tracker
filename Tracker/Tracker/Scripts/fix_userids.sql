-- Fix UserId mismatch: Seed script used UserId=10 but actual user has id=1
-- Run as tracker_app user

UPDATE "TeamMembers" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "OneOnOnes" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "Projects" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "Tasks" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "MeetingTasks" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "AgendaItems" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "ObjectiveKeyResults" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "KeyResults" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "KeyPerformanceIndicators" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "KeyResultMeasurables" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "TaskCollections" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "TaskCollectionItems" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "Milestones" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "Risks" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "Feedbacks" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "IndividualGoals" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "GoalMilestones" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "Reminders" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "QuickNotes" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "Kudos" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "ManagerHistory" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "ProjectDependencies" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "MeetingTemplates" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "MeetingTemplateItems" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "OneOnOneLinkedTasks" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "OneOnOneLinkedOkrs" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "OneOnOneLinkedKpis" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "CalendarLinks" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "PerformanceReviewCycles" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "PerformanceReviews" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "ReviewTemplates" SET "UserId" = 1 WHERE "UserId" = 10;
UPDATE "PulseSurveys" SET "UserId" = 1 WHERE "UserId" = 10;

-- Verify the fix
SELECT 'TeamMembers' as tbl, COUNT(*) as cnt FROM "TeamMembers" WHERE "UserId" = 1
UNION ALL
SELECT 'OneOnOnes', COUNT(*) FROM "OneOnOnes" WHERE "UserId" = 1;
