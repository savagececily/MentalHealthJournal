import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { AppInsightsContext } from '@microsoft/applicationinsights-react-js'
import { reactPlugin } from './appInsights'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppInsightsContext.Provider value={reactPlugin}>
      <App />
    </AppInsightsContext.Provider>
  </StrictMode>,
)
