# TTB Label Review Prototype

Automated label-to-application verification tool built for the Alcohol
and Tobacco Tax and Trade Bureau (TTB), which processes roughly 150,000
Certificate of Label Approval (COLA) submissions a year. This prototype
demonstrates fast OCR-based extraction, field comparison, and batch
processing to reduce manual workload for compliance agents reviewing
COLA submissions.

- ASP.NET Core Web API (.NET 8)
- Tesseract OCR (local, no outbound ML calls)
- SixLabors.ImageSharp for preprocessing
- React frontend (Vite)
- Azure App Service deployment target

## Deployment

_________________________________________________________________________________________________________
FRONTEND UI TEST: https://ttb-label-review-ui-yourname.onrender.com
BACKEND TEST: https://ttb-label-review-api-yourname.onrender.com/swagger/index.html
_________________________________________________________________________________________________________

## Features

- Upload label images (JPG/PNG/PDF)
- Extract text using local, offline OCR
- Identify key TTB-required fields:
  - Brand name (match check exactly, no technical error)
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

> **No authentication.** The API and UI have no login or access control,
> since this is a local prototype. A real deployment would need this
> before handling actual pre-publication label artwork.

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- Tesseract installed locally, with trained language data available
  (set the path via `Ocr:TessDataPath` in `appsettings.json`)
- Azure CLI (only needed for deployment)

## Getting started

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
- Government Health Warning Statement (mandatory on all alcohol
  beverages; must appear in capital letters and bold type per TTB
  regulations)

## Tech stack

See the stack summary at the top of this document.

## Further reading

- [`docs/approach.md`](docs/approach.md) — technical approach and pipeline design

## License

See [LICENSE](LICENSE).
