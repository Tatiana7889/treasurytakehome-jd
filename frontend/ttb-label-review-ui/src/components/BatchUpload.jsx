import { useState } from 'react'

export default function BatchUpload({ onSubmit, isSubmitting, error }) {
  const [zipFile, setZipFile] = useState(null)
  const [csvFile, setCsvFile] = useState(null)

  function handleSubmit(event) {
    event.preventDefault()
    if (!zipFile || !csvFile) return
    onSubmit(zipFile, csvFile)
  }

  return (
    <form className="panel" onSubmit={handleSubmit}>
      <div className="field-group">
        <label htmlFor="zip-input">
          Label images (ZIP)
          <span className="hint">A single ZIP archive containing all label image files for this batch.</span>
        </label>
        <input
          id="zip-input"
          type="file"
          accept=".zip"
          onChange={(e) => setZipFile(e.target.files?.[0] ?? null)}
        />
      </div>

      <div className="field-group">
        <label htmlFor="csv-input">
          Manifest (CSV)
          <span className="hint">
            One row per label, mapping each file name to its application data. Columns: FileName, BrandName,
            ClassType, AlcoholContent, NetContents, BottlerOrProducerName, BottlerOrProducerAddress,
            CountryOfOrigin, BeverageType.
          </span>
        </label>
        <input
          id="csv-input"
          type="file"
          accept=".csv"
          onChange={(e) => setCsvFile(e.target.files?.[0] ?? null)}
        />
      </div>

      {error && <p className="error-text">{error}</p>}

      <div className="form-actions">
        <button type="submit" className="primary" disabled={!zipFile || !csvFile || isSubmitting}>
          {isSubmitting ? 'Processing batch…' : 'Process batch'}
        </button>
      </div>
    </form>
  )
}
