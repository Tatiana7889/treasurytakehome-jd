# TTB Label Review Prototype

Automated label-to-application verification tool built for the Alcohol
and Tobacco Tax and Trade Bureau (TTB) which is runing 150K a year. This prototype demonstrates fast OCR-based extraction, field comparison, and batch processing to reduce
manual workload for compliance agents reviewing Certificate of Label
Approval (COLA) submissions.

- ASP.NET Core Web API (.NET 8)
- Tesseract OCR (local, no outbound ML calls)
- SixLabors.ImageSharp for preprocessing
- React frontend (Vite)
- Azure App Service deployment target

## Features

- Upload label images (JPG/PNG/PDF)
- Extract text using local, offline OCR
- Identify key TTB-required fields:
  - Brand name ( match check exactly, no technical error)
  - Class/type
  - Alcohol content
  - Net contents
  - Bottler/producer name and address
  - Country of origin (for imports)
  - Government Warning statement
- Compare extracted values to application data
- Highlight mismatches with reasoning, not just a pass/fail flag
- Batch upload support (ZIP of images + CSV manifest)
- Fast processing (typically under 5 seconds per label)
- Simple UI designed for non-technical reviewing agents

##  **No authentication.** The API and UI have no login or access control,
  since this is a local prototype. A real deployment would need this
  before handling actual pre-publication label artwork.

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- Tesseract installed locally, with trained language data available
  (set the path via `Ocr:TessDataPath` in `appsettings.json`)
- Azure CLI (only needed for deployment)

## Getting started
## It exposes:

proxy traffic from Vite → backend unless the backend port is whitelisted

use https://localhost:<port> unless the firewall allows outbound TLS

This is why:
HTTPS → blocked

HTTP → allowed
### Backend

```bash
cd src/Ttb.LabelReview.Api
dotnet restore
dotnet run
```

The API listens on the port shown in the console output and serves
Swagger UI at `/swagger` in the Development environment. See
[`docs/api-spec.md`](docs/api-spec.md) for endpoint details.

### Frontend

```bash
cd frontend/ttb-label-review-ui
npm install
npm run dev
```

The dev server proxies `/api` requests to the backend (see
`vite.config.js`); update the proxy target if your API runs on a
different port.

### Running tests

```bash
cd src/Ttb.LabelReview.Tests
dotnet test
```

## TTB label requirements (reference)

TTB requires specific information on alcohol beverage labels. Exact
requirements vary by beverage type (beer, wine, distilled spirits), but
common elements include:

- Brand name
- Class/type designation
- Alcohol content (with some exceptions for certain wine/beer)
- Net contents
- Name and address of bottler/producer
- Country of origin for imports
- Government Health Warning Statement (mandatory on all alcohol beverages UPPER CASE ONLY and BOOLD)

## `GET /health`

Simple liveness check, not under the `/api` prefix.

```json
{ "status": "healthy", "timestampUtc": "2026-08-31T14:00:00Z" }
```
# API specification

Base URL (local development): `http://localhost:5000/swagger/index.html`

Interactive Swagger UI is available at `/swagger` when running in the
`Development` environment.

## `POST /api/LabelAnalysis/analyze`

Analyzes a single label image against supplied application data.

**Content type:** `frontend/Images`

 `Images` | file | yes | JPG, PNG, or PDF of the label artwork |
 `applicationDataJson` | string (JSON) | yes | Serialized `ApplicationData` object |

**`ApplicationData` JSON shape:**

```json
{
  "brandName": "Bold Whiskey",
  "classType": "Straight Bourbon Whiskey",
  "alcoholContent": "45% ALC/VOL",
  "netContents": "750 mL",
  "bottlerOrProducerName": "Acme Distillery",
  "bottlerOrProducerAddress": "123 Main St, Louisville, KY",
  "countryOfOrigin": null,
  "governmentWarningText": "GOVERNMENT WARNING: ...",
  "beverageType": "DistilledSpirits"
}
```

**Response `200 OK`:** `LabelAnalysisResult`

```json
{
  "labelFileName": "bold-whiskey-front.jpg",
  "rawOcrText": "BOLD WHISKEY STRAIGHT BOURBON WHISKEY 45% ALC/VOL 750 mL ...",
  "ocrConfidence": 0.91,
  "fieldResults": [
    {
      "fieldName": "Alcohol Content",
      "expectedValue": "45% ALC/VOL",
      "foundValue": "45% ALC/VOL",
      "status": "Match",
      "similarityScore": 1.0,
      "reasoning": "Alcohol Content on the label matches the application.",
      "isRequiredField": true
    }
  ],
  "outcome": "Pass",
  "processedAtUtc": "2026-08-31T14:02:11Z",
  "processingTimeMs": 812,
  "warnings": []
}
```

`status` is one of: `Match`, `PartialMatch`, `Mismatch`, `NotFound`.
`outcome` is one of: `Pass`, `Fail`, `NeedsManualReview`.
## Tech stack

## Further reading

- [`docs/approach.md`](docs/approach.md) — technical approach and pipeline design


## License

See [LICENSE](LICENSE).
