import React, { useState } from 'react'
import ConversionPage from './pages/ConversionPage'
import AuditHistoryPage from './pages/AuditHistoryPage'

export default function App() {
  const [activeTab, setActiveTab] = useState<'convert' | 'history'>('convert')

  return (
    <div style={{ maxWidth: 980, margin: '24px auto', padding: 16, fontFamily: 'system-ui, sans-serif' }}>
      <h1 style={{ margin: 0, fontSize: 20 }}>Currency Conversion & Audit Trail</h1>
      <div style={{ display: 'flex', gap: 8, marginTop: 16 }}>
        <button
          type="button"
          onClick={() => setActiveTab('convert')}
          style={{
            padding: '8px 12px',
            borderRadius: 8,
            border: '1px solid #ccc',
            background: activeTab === 'convert' ? '#111' : '#fff',
            color: activeTab === 'convert' ? '#fff' : '#111',
            cursor: 'pointer',
          }}
        >
          Convert
        </button>
        <button
          type="button"
          onClick={() => setActiveTab('history')}
          style={{
            padding: '8px 12px',
            borderRadius: 8,
            border: '1px solid #ccc',
            background: activeTab === 'history' ? '#111' : '#fff',
            color: activeTab === 'history' ? '#fff' : '#111',
            cursor: 'pointer',
          }}
        >
          Audit History
        </button>
      </div>

      <div style={{ marginTop: 18 }}>
        {activeTab === 'convert' ? <ConversionPage /> : <AuditHistoryPage />}
      </div>
    </div>
  )
}
