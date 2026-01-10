import { ApplicationInsights, type ICustomProperties } from '@microsoft/applicationinsights-web';
import { ReactPlugin } from '@microsoft/applicationinsights-react-js';

const reactPlugin = new ReactPlugin();

// Get connection string from environment
const connectionString = import.meta.env.VITE_APPLICATIONINSIGHTS_CONNECTION_STRING || 
                         // Fallback: try to get it from the backend's config (set by Azure)
                         (window as Window & { appInsightsConnectionString?: string }).appInsightsConnectionString;

// Define a subset of ApplicationInsights methods that are actually used
interface IAppInsights {
    trackEvent(event: { name: string; properties?: ICustomProperties }): void;
    trackException(exception: { exception: Error; properties?: ICustomProperties }): void;
    trackPageView(): void;
    trackTrace(trace: { message: string; properties?: ICustomProperties }): void;
    trackMetric(metric: { name: string; average: number; properties?: ICustomProperties }): void;
    flush(): void;
}

// Create a no-op implementation that matches the interface
const createNoOpAppInsights = (): IAppInsights => ({
    trackEvent: () => {},
    trackException: () => {},
    trackPageView: () => {},
    trackTrace: () => {},
    trackMetric: () => {},
    flush: () => {},
});

let appInsights: ApplicationInsights | IAppInsights;

// Only initialize Application Insights if we have a connection string
if (connectionString) {
    const aiInstance = new ApplicationInsights({
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
    aiInstance.loadAppInsights();
    appInsights = aiInstance;
    console.log('Application Insights initialized');
} else {
    console.warn('Application Insights connection string not found - telemetry disabled');
    appInsights = createNoOpAppInsights();
}

export { reactPlugin, appInsights };
