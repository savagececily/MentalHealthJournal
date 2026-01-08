import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { ReactPlugin } from '@microsoft/applicationinsights-react-js';

const reactPlugin = new ReactPlugin();

// Get connection string from environment
const connectionString = import.meta.env.VITE_APPLICATIONINSIGHTS_CONNECTION_STRING || 
                         // Fallback: try to get it from the backend's config (set by Azure)
                         (window as any).appInsightsConnectionString;

let appInsights: ApplicationInsights;

// Only initialize Application Insights if we have a connection string
if (connectionString) {
    appInsights = new ApplicationInsights({
        config: {
            connectionString: connectionString,
            extensions: [reactPlugin],
            enableAutoRouteTracking: true, // Track route changes automatically
            disableFetchTracking: false,   // Track fetch/XHR requests
            enableCorsCorrelation: true,   // Correlate client and server telemetry
            enableRequestHeaderTracking: true,
            enableResponseHeaderTracking: true,
            autoTrackPageVisitTime: true,  // Track time spent on pages
        }
    });
    appInsights.loadAppInsights();
    console.log('Application Insights initialized');
} else {
    console.warn('Application Insights connection string not found - telemetry disabled');
    // Create a no-op instance to prevent errors when tracking methods are called
    appInsights = {
        trackEvent: () => {},
        trackException: () => {},
        trackPageView: () => {},
        trackTrace: () => {},
        trackMetric: () => {},
        trackDependencyData: () => {},
        flush: () => {},
    } as any;
}

export { reactPlugin, appInsights };
