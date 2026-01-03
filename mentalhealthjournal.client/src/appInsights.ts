import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { ReactPlugin } from '@microsoft/applicationinsights-react-js';

const reactPlugin = new ReactPlugin();

// Initialize Application Insights
// The connection string will be automatically picked up from the environment
const appInsights = new ApplicationInsights({
    config: {
        connectionString: import.meta.env.VITE_APPLICATIONINSIGHTS_CONNECTION_STRING || 
                         // Fallback: try to get it from the backend's config (set by Azure)
                         (window as any).appInsightsConnectionString,
        extensions: [reactPlugin],
        enableAutoRouteTracking: true, // Track route changes automatically
        disableFetchTracking: false,   // Track fetch/XHR requests
        enableCorsCorrelation: true,   // Correlate client and server telemetry
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true,
        autoTrackPageVisitTime: true,  // Track time spent on pages
    }
});

// Only load if we have a connection string
if (appInsights.config.connectionString) {
    appInsights.loadAppInsights();
    console.log('Application Insights initialized');
} else {
    console.warn('Application Insights connection string not found');
}

export { reactPlugin, appInsights };
