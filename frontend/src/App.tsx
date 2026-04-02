import { useState, useRef, useCallback } from 'react'
import './App.css'

interface EmployeePairResult {
  employee1Id: number
  employee2Id: number
  projectId: number
  daysWorked: number
}

type Status = 'idle' | 'loading' | 'done' | 'error'

export default function App() {
  const [file, setFile]           = useState<File | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [status, setStatus]       = useState<Status>('idle')
  const [results, setResults]     = useState<EmployeePairResult[]>([])
  const [errorMsg, setErrorMsg]   = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  const pickFile = useCallback((f: File) => {
    setFile(f)
    setStatus('idle')
    setResults([])
    setErrorMsg('')
  }, [])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    const f = e.dataTransfer.files[0]
    if (f) pickFile(f)
  }, [pickFile])

  const analyze = async () => {
    if (!file) return
    setStatus('loading')
    setResults([])
    setErrorMsg('')

    try {
      const form = new FormData()
      form.append('file', file)

      const res = await fetch('/api/employees/upload', { method: 'POST', body: form })

      if (!res.ok) {
        let msg = `Server error (${res.status})`
        try {
          const err = await res.json()
          if (err.message) msg = err.message
        } catch { /* keep default */ }
        throw new Error(msg)
      }

      const data: EmployeePairResult[] = await res.json()
      setResults(data)
      setStatus('done')
    } catch (e) {
      setErrorMsg(e instanceof Error ? e.message : 'An unexpected error occurred.')
      setStatus('error')
    }
  }

  const totalDays = results.reduce((s, r) => s + r.daysWorked, 0)

  return (
    <div className="app">
      <header className="page-header">
        <h1>Employee Pair Analyzer</h1>
        <p>Identify the pair of employees who worked together the longest</p>
      </header>

      <section className="card">
        <div
          className={`drop-zone${isDragging ? ' drag-over' : ''}${file ? ' has-file' : ''}`}
          onClick={() => inputRef.current?.click()}
          onDragOver={e => { e.preventDefault(); setIsDragging(true) }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={handleDrop}
          role="button"
          tabIndex={0}
          aria-label="Upload CSV file"
          onKeyDown={e => e.key === 'Enter' && inputRef.current?.click()}
        >
          <input
            ref={inputRef}
            type="file"
            accept=".csv,text/csv"
            style={{ display: 'none' }}
            onChange={e => { const f = e.target.files?.[0]; if (f) pickFile(f) }}
          />
          {file ? (
            <div className="file-selected">
              <span className="file-icon">&#x1F4C4;</span>
              <div>
                <div className="file-name">{file.name}</div>
                <div className="file-meta">{(file.size / 1024).toFixed(1)} KB &middot; Click to change</div>
              </div>
            </div>
          ) : (
            <div className="drop-hint">
              <span className="drop-arrow">&#x2B06;</span>
              <p>Drop a CSV file here or <span className="link-text">click to browse</span></p>
              <p className="format-hint">Format: EmpID, ProjectID, DateFrom, DateTo</p>
            </div>
          )}
        </div>

        {file && (
          <button
            className="btn-analyze"
            onClick={analyze}
            disabled={status === 'loading'}
          >
            {status === 'loading' ? 'Analyzing\u2026' : 'Analyze'}
          </button>
        )}
      </section>

      {status === 'error' && (
        <div className="alert alert-error" role="alert">{errorMsg}</div>
      )}

      {status === 'done' && results.length === 0 && (
        <div className="alert alert-info" role="status">
          No overlapping work periods found in the uploaded file.
        </div>
      )}

      {results.length > 0 && (
        <section className="card results-card">
          <div className="summary-bar">
            <div className="summary-item">
              <span className="summary-label">Top pair</span>
              <span className="summary-value">
                Employee {results[0].employee1Id} &amp; Employee {results[0].employee2Id}
              </span>
            </div>
            <div className="summary-divider" />
            <div className="summary-item">
              <span className="summary-label">Total days together</span>
              <span className="summary-value">{totalDays}</span>
            </div>
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Employee ID #1</th>
                  <th>Employee ID #2</th>
                  <th>Project ID</th>
                  <th>Days Worked</th>
                </tr>
              </thead>
              <tbody>
                {results.map((r, i) => (
                  <tr key={i}>
                    <td>{r.employee1Id}</td>
                    <td>{r.employee2Id}</td>
                    <td>{r.projectId}</td>
                    <td>{r.daysWorked}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </div>
  )
}
