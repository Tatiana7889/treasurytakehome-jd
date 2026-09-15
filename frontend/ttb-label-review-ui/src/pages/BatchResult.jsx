import { useState } from 'react'
import { useLocation, Link, useNavigate } from 'react-router-dom'
import ResultSummary from '../components/ResultSummary.jsx'
import FieldComparisonCard from '../components/FieldComparisonCard.jsx'

export default function BatchResult() {
  const location = useLocation()
  const navigate = useNavigate()
  const batchResult = location.state?.batchResult
  const [selectedLabel, setSelectedLabel] = useState(null)

  if (!batchResult) {
    return (
      <div className="page-intro">
        <h2>No batch to show</h2>
        <p>Process a batch first, then its summary will appear here.</p>
        <Link to="/batch">Back to batch upload</Link>
      </div>
    )
  }

  return (
    <div>
      <div className="page-intro">
        <h2>Batch results</h2>
        <p>{batchResult.totalLabels} labels submitted · completed {new Date(batchResult.completedAtUtc).toLocaleString()}</p>
      </div>

      {selectedLabel ? (
        <div>
          <button className="secondary" style={{ marginBottom: '1.5rem' }} onClick={() => setSelectedLabel(null)}>
            ← Back to batch summary
          </button>
          <div className="page-intro">
            <h2 style={{ fontSize: '1.4rem' }}>{selectedLabel.labelFileName}</h2>
          </div>
          <div className="panel">
            {selectedLabel.fieldResults.map((fieldResult) => (
              <FieldComparisonCard key={fieldResult.fieldName} result={fieldResult} />
            ))}
          </div>
        </div>
      ) : (
        <ResultSummary batchResult={batchResult} onSelectLabel={setSelectedLabel} />
      )}

      {!selectedLabel && (
        <div className="form-actions" style={{ justifyContent: 'flex-start', marginTop: '2rem' }}>
          <button className="secondary" onClick={() => navigate('/batch')}>
            Process another batch
          </button>
        </div>
      )}
    </div>
  )
}
