import PropTypes from "prop-types";

const STATUS_LABELS = {
  Match: "Match",
  PartialMatch: "Partial match",
  Mismatch: "Mismatch",
  NotFound: "Not found",

  0: "Match",
  1: "Partial match",
  2: "Mismatch",
  3: "Not found"
};

export default function FieldComparisonCard({ result }) {
  const rawStatus = result?.status ?? "";
  const statusClass = String(rawStatus).toLowerCase();

  return (
    <div className="field-card">
      <div className="field-name">{result.fieldName}</div>

      <div className="values">
        <span className="expected">Expected: {result.expectedValue || "—"}</span>
        <br />
        <span>Found: {result.foundValue || "not detected"}</span>
        {result.reasoning && <div className="reasoning">{result.reasoning}</div>}
      </div>

      <span className={`status-pill ${statusClass}`}>
        {STATUS_LABELS[rawStatus] ?? rawStatus}
      </span>
    </div>
  );
}

FieldComparisonCard.propTypes = {
  result: PropTypes.shape({
    status: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
    fieldName: PropTypes.string.isRequired,
    expectedValue: PropTypes.string,
    foundValue: PropTypes.string,
    reasoning: PropTypes.string
  }).isRequired
};
