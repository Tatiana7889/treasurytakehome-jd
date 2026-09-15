import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import UploadForm from '../components/UploadForm.jsx'
import { analyzeLabel } from '../api/client.js'

export default function Home() {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const navigate = useNavigate()

  async function handleSubmit(file, formData) {
    setIsSubmitting(true)
    setError(null)
    try {
      const result = await analyzeLabel(file, formData)
      navigate('/result', { state: { result } })
    } catch (err) {
      setError(err.message || 'Something went wrong while analyzing the label.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div>
      <div className="page-intro">
        <h2>Review a single label</h2>
        <p>
          Upload a label image and the values from the corresponding application. The tool runs local OCR,
          extracts the TTB-required fields, and flags anything that doesn&#39;t match..
        </p>
      </div>
      <UploadForm onSubmit={handleSubmit} isSubmitting={isSubmitting} error={error} />
    </div>
  )
}
