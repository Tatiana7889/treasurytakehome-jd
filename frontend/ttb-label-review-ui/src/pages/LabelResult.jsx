import { useLocation, useNavigate, Link } from 'react-router-dom'
import FieldComparisonCard from '../components/FieldComparisonCard.jsx'

const OUTCOME_COPY = {
  Pass: { label: 'All required fields match', className: 'pass' },
  Fail: { label: 'One or more required fields do not match', className: 'fail' },
  NeedsManualReview: { label: 'Needs manual review', className: 'review' },
}

export default function LabelResult() {
  const location = useLocation()
  const navigate = useNavigate()
  const result = location.state?.result

  if (!result) {
    return (
      <div className="page-intro">
        <h2>No result to show</h2>
        <p>Analyze a label first, then its results will appear here.</p>
        <Link to="/">Back to upload</Link>
      </div>
    )
  }

  const outcome = OUTCOME_COPY[result.outcome] ?? { label: result.outcome, className: 'review' }

  return (
    <div>
      <div className="page-intro">
        <h2>{result.labelFileName}</h2>
        <p>Processed in {result.processingTimeMs}ms · OCR confidence {(result.ocrConfidence * 100).toFixed(0)}%</p>
      </div>

      <div className={`outcome-banner ${outcome.className}`}>
        {outcome.label}
        <span className="meta">
          {new Date(result.processedAtUtc).toLocaleString()}
        </span>
      </div>

      {result.warnings?.length > 0 && (
        <div className="warning-box">
          {result.warnings.map((warning, i) => (
            <div key={i}>{warning}</div>
          ))}
        </div>
      )}

      <div className="panel">
        {result.fieldResults.map((fieldResult) => (
          <FieldComparisonCard key={fieldResult.fieldName} result={fieldResult} />
        ))}
      </div>

      <div className="form-actions" style={{ justifyContent: 'flex-start', marginTop: '2rem' }}>
        <button className="secondary" onClick={() => navigate('/')}>
          Analyze another label
        </button>
      </div>
    </div>
  )
}
