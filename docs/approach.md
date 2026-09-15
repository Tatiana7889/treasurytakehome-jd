# Approach

## Problem framing

A TTB compliance agent reviewing a Certificate of Label Approval (COLA)
application has to check that the artwork submitted for a label actually
says what the applicant claimed on the application: the right brand name,
class/type, alcohol content, net contents, bottler/producer information,
country of origin (for imports), and the mandatory Government Warning
statement. Today this is done by eye, label by label. This prototype
automates the mechanical part of that check — extracting the label's text
and comparing it field-by-field against the application — so an agent can
spend their attention on genuinely ambiguous cases instead of re-reading
every label from scratch.

The prototype is explicitly scoped as a decision **aid**, not a decision
**maker**. Every result includes an OCR confidence score, per-field
reasoning, and an overall recommendation of Pass / Fail / Needs Manual
Review — but the "Needs Manual Review" bucket is deliberately generous
(low OCR confidence or a partial text match routes there rather than
being force-fit into Pass or Fail).

## Pipeline

1. **Image preprocessing** (`ImagePreprocessingService`) — upscales small
   images, converts to grayscale, boosts contrast, and binarizes. Label
   photos submitted by applicants vary wildly in lighting and resolution;
   this step accounts for most of the difference between unusable and
   usable OCR output.
2. **OCR** (`OcrService`) — runs Tesseract locally against the
   preprocessed image. No image or extracted text leaves the machine,
   which matters for handling pre-publication label artwork.
3. **Field extraction** (`FieldExtractionService`) — pulls structured
   values (alcohol content, net contents, presence of the Government
   Warning header, bottler/producer line, country of origin) out of the
   raw OCR text using pattern matching tuned to how these fields
   conventionally appear on labels.
4. **Comparison** (`ComparisonService`) — compares each extracted or
   located value against the corresponding application field. Structured
   fields (alcohol content, net contents) use exact/normalized
   comparison; free-text fields (brand name, class/type, bottler name)
   use a fuzzy containment check, since these don't follow a fixed
   format and OCR line-wrapping can split them oddly.
5. **Outcome determination** — a label passes only if every required
   field matched; any mismatch or missing required field fails it; any
   partial match or low OCR confidence sends it to manual review instead
   of guessing.

## Batch processing

The batch endpoint accepts a ZIP of label images and a CSV manifest that
maps each file name to its application data, so an agent can process an
entire day's submissions in one request instead of one label at a time.
Rows referencing a file that isn't in the ZIP are recorded as skipped
rather than failing the whole batch.

## Why these tools

- **Tesseract** — mature, free, runs entirely offline, and is more than
  accurate enough for the mostly-clean, mostly-Latin-script text that
  appears on alcohol labels. No outbound calls to a hosted OCR/ML API
  were used, since label artwork may be commercially sensitive and
  pre-publication.
- **Regex-based extraction over an ML/LLM field extractor** — for a
  prototype, deterministic, auditable pattern matching is easier to
  debug, easier to explain to a non-technical reviewer, and doesn't
  require a model dependency. The trade-off is coverage: unusual label
  layouts will need new patterns added over time. See
  `docs/assumptions.md` for where this is expected to fall short.
