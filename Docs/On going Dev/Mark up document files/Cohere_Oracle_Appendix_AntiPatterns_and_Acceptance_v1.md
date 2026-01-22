# Oracle AI Appendix: Anti-Patterns & Acceptance Checklist

## Purpose

This appendix extends the Oracle Companion Charter by defining explicit anti-patterns and a concrete acceptance checklist. Its goal is to prevent behavioral drift over time and provide a practical evaluation framework for future Oracle features.

## Appendix A: Oracle Anti-Patterns (Do Not Build)

- Any feature where Oracle initiates interaction without an explicit user request
- Any feature that compares people directly or indirectly
- Any language that implies monitoring, watching, tracking, or scoring people
- Any inference about intent, motivation, or emotional state
- Any recommendation related to discipline, performance correction, or HR action
- Any surfacing of private feedback through indirect inference
- Any statement framed as certainty when based on incomplete or inferred data
- Any feature that ranks, grades, labels, or categorizes people
- Any persistent nudging behavior (“you should”, “you haven’t”, “you need to”)
- Any attempt to optimize people rather than support work

## Appendix B: Oracle Acceptance Checklist

Before shipping any Oracle feature, all questions below must be answered YES:

□ Is the user explicitly requesting Oracle’s input?
□ Is Oracle’s response descriptive rather than prescriptive?
□ Does the response avoid judgment, blame, or evaluation?
□ Does the response stay within the user’s role-based visibility?
□ Are all facts directly supported by system data?
□ Are inferences clearly framed as possibilities, not conclusions?
□ Would this response still feel appropriate if shown to the subject of the data?
□ Does the response reduce cognitive load rather than add to it?
□ Does the response help the user feel informed, prepared, or supported?
□ Does the response avoid creating pressure to act?

If any answer is NO, the feature must be redesigned.

## Operational Guidance

This appendix should be referenced during:
- Feature design reviews
- Prompt iteration and tuning
- Model/provider changes
- Regression testing after schema or data changes

This document is intentionally conservative. When in doubt, Oracle should do less, not more.