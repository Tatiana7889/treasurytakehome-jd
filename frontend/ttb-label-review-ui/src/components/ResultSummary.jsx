const OUTCOME_LABELS = {
  Pass: 'Pass',
  Fail: 'Fail',
  NeedsManualReview: 'Needs review',
}


export default function ResultSummary({ batchResult, onSelectLabel }) {
  return (
    <div>
      <div className="summary-stats">
        <div className="stat pass">
          <span className="count">{batchResult.passCount}</span>
          Passed
        </div>
        <div className="stat fail">
          <span className="count">{batchResult.failCount}</span>
          Failed
        </div>
        <div className="stat review">
          <span className="count">{batchResult.needsReviewCount}</span>
          Needs review
        </div>
      </div>

      {batchResult.skippedFiles?.length > 0 && (
        <div className="warning-box">
          {batchResult.skippedFiles.length} file(s) listed in the manifest were not found in the ZIP archive:{' '}
          {batchResult.skippedFiles.join(', ')}
        </div>
      )}

      <table className="batch-table">
        <thead>
          <tr>
            <th>Label file</th>
            <th>Outcome</th>
            <th>OCR confidence</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {batchResult.results.map((result) => (
            <tr key={result.labelFileName}>
              <td>{result.labelFileName}</td>
              <td>
                <span className={`status-pill ${result.outcome === 'NeedsManualReview' ? 'partialmatch' : result.outcome.toLowerCase()}`}>
                  {OUTCOME_LABELS[result.outcome] ?? result.outcome}
                </span>
              </td>
              <td>{(result.ocrConfidence * 100).toFixed(0)}%</td>
              <td>
                <button className="secondary" onClick={() => onSelectLabel(result)}>
                  View detail
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
