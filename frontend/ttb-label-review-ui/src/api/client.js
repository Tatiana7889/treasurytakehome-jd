const BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

async function handleResponse(response) {
  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(text || `Request failed with status ${response.status}`)
  }
  return response.json()
}

/**
 * Submits a single label image plus application data for analysis.
 * @param {File} labelImageFile
 * @param {object} applicationData
 */
export async function analyzeLabel(labelImageFile, applicationData) {
  const formData = new FormData()
  formData.append('labelImage', labelImageFile)
  formData.append('applicationDataJson', JSON.stringify(applicationData))

  const response = await fetch(`${BASE_URL}/LabelAnalysis/analyze`, {
    method: 'POST',
    body: formData,
  })
  console.log("labelImageFile", labelImageFile)
  console.log("BASE_URL", BASE_URL)
  console.log("JSON SENT:", JSON.stringify(applicationData))
  for (const [key, value] of formData.entries()) {
    console.log("FORM FIELD:", key, value)
  }

  return handleResponse(response)
}

/**
 * Submits a ZIP of label images plus a CSV manifest for batch processing.
 * @param {File} zipFile
 * @param {File} csvFile
 */
export async function processBatch(zipFile, csvFile) {
  const formData = new FormData()
  formData.append('labelsZip', zipFile)
  formData.append('manifestCsv', csvFile)

  const response = await fetch(`${BASE_URL}/Batch/process`, {
    method: 'POST',
    body: formData,
  })

  return handleResponse(response)
}
