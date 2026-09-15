import { useState } from 'react'

const initialData = {
  brandName: '',
  classType: '',
  alcoholContent: '',
  netContents: '',
  bottlerOrProducerName: '',
  bottlerOrProducerAddress: '',
  countryOfOrigin: '',
  beverageType: 'DistilledSpirits',
}


export default function UploadForm({ onSubmit, isSubmitting, error }) {
  const [file, setFile] = useState(null)
  const [isDragActive, setIsDragActive] = useState(false)
  const [formData, setFormData] = useState(initialData)

  function updateField(field, value) {
    setFormData((prev) => ({ ...prev, [field]: value }))
  }

  function handleDrop(event) {
    event.preventDefault()
    setIsDragActive(false)
    const dropped = event.dataTransfer.files?.[0]
    if (dropped) setFile(dropped)
  }

  function handleSubmit(event) {
    event.preventDefault()
    if (!file) return
    onSubmit(file, formData)
  }

  return (
    <form className="panel" onSubmit={handleSubmit}>
      <div
        className={`dropzone ${isDragActive ? 'active' : ''}`}
        onDragOver={(e) => { e.preventDefault(); setIsDragActive(true) }}
        onDragLeave={() => setIsDragActive(false)}
        onDrop={handleDrop}
        onClick={() => document.getElementById('label-file-input').click()}
      >
        {file ? (
          <span className="filename">{file.name}</span>
        ) : (
          <span>Drag a label image here, or click to browse (JPG, PNG, or PDF)</span>
        )}
        <input
          id="label-file-input"
          type="file"
          accept=".jpg,.jpeg,.png,.pdf"
          style={{ display: 'none' }}
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
      </div>

      <div className="field-group" style={{ marginTop: '1.75rem' }}>
        <label htmlFor="beverageType">Beverage type</label>
        <select
          id="beverageType"
          value={formData.beverageType}
          onChange={(e) => updateField('beverageType', e.target.value)}
        >
          <option value="DistilledSpirits">Distilled spirits</option>
          <option value="Wine">Wine</option>
          <option value="Beer">Beer</option>
        </select>
      </div>

      <div className="field-row">
        <div className="field-group">
          <label htmlFor="brandName">Brand name</label>
          <input
            id="brandName"
            type="text"
            value={formData.brandName}
            onChange={(e) => updateField('brandName', e.target.value)}
            required
          />
        </div>
        <div className="field-group">
          <label htmlFor="classType">Class / type</label>
          <input
            id="classType"
            type="text"
            value={formData.classType}
            onChange={(e) => updateField('classType', e.target.value)}
            placeholder="e.g. Straight Bourbon Whiskey"
            required
          />
        </div>
      </div>

      <div className="field-row">
        <div className="field-group">
          <label htmlFor="alcoholContent">Alcohol content</label>
          <input
            id="alcoholContent"
            type="text"
            value={formData.alcoholContent}
            onChange={(e) => updateField('alcoholContent', e.target.value)}
            placeholder="e.g. 45% ALC/VOL"
          />
        </div>
        <div className="field-group">
          <label htmlFor="netContents">Net contents</label>
          <input
            id="netContents"
            type="text"
            value={formData.netContents}
            onChange={(e) => updateField('netContents', e.target.value)}
            placeholder="e.g. 750 mL"
          />
        </div>
      </div>

      <div className="field-group">
        <label htmlFor="bottlerName">Bottler / producer name</label>
        <input
          id="bottlerName"
          type="text"
          value={formData.bottlerOrProducerName}
          onChange={(e) => updateField('bottlerOrProducerName', e.target.value)}
        />
      </div>

      <div className="field-group">
        <label htmlFor="bottlerAddress">Bottler / producer address</label>
        <input
          id="bottlerAddress"
          type="text"
          value={formData.bottlerOrProducerAddress}
          onChange={(e) => updateField('bottlerOrProducerAddress', e.target.value)}
        />
      </div>

      <div className="field-group">
        <label htmlFor="countryOfOrigin">
          Country of origin
          <span className="hint">Leave blank for domestically produced beverages.</span>
        </label>
        <input
          id="countryOfOrigin"
          type="text"
          value={formData.countryOfOrigin}
          onChange={(e) => updateField('countryOfOrigin', e.target.value)}
        />
      </div>

      {error && <p className="error-text">{error}</p>}

      <div className="form-actions">
        <button type="submit" className="primary" disabled={!file || isSubmitting}>
          {isSubmitting ? 'Analyzing label…' : 'Analyze label'}
        </button>
      </div>
    </form>
  )
}
