import { useState } from 'react'
import { Routes, Route, NavLink, useNavigate } from 'react-router-dom'
import Home from './pages/Home.jsx'
import LabelResult from './pages/LabelResult.jsx'
import BatchResult from './pages/BatchResult.jsx'
import BatchUpload from './components/BatchUpload.jsx'
import { processBatch } from './api/client.js'

function BatchPage() {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const navigate = useNavigate()

  async function handleSubmit(zipFile, csvFile) {
    setIsSubmitting(true)
    setError(null)
    try {
      const batchResult = await processBatch(zipFile, csvFile)
      navigate('/batch/result', { state: { batchResult } })
    } catch (err) {
      setError(err.message || 'Something went wrong while processing the batch.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div>
      <div className="page-intro">
        <h2>Review a batch of labels</h2>
        <p>
          Upload a ZIP of label images alongside a CSV manifest of application data, and every label in the
          batch will be analyzed and scored in one pass.
        </p>
      </div>
      <BatchUpload onSubmit={handleSubmit} isSubmitting={isSubmitting} error={error} />
    </div>
  )
}

export default function App() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand">
          <h1>TTB Label Review</h1>
          <span className="agency-tag">Compliance prototype</span>
        </div>
        <nav className="app-nav">
          <NavLink to="/" end className={({ isActive }) => (isActive ? 'active' : '')}>
            Single label
          </NavLink>
          <NavLink to="/batch" className={({ isActive }) => (isActive ? 'active' : '')}>
            Batch upload
          </NavLink>
        </nav>
      </header>

      <main className="app-main">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/result" element={<LabelResult />} />
          <Route path="/batch" element={<BatchPage />} />
          <Route path="/batch/result" element={<BatchResult />} />
        </Routes>
      </main>
    </div>
  )
}
