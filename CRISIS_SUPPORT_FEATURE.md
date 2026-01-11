# Crisis Support Integration

## Overview

The Mental Health Journal application now includes an intelligent crisis detection and support system designed to provide immediate access to professional resources when users may be experiencing mental health crises.

## Features

### 1. AI-Powered Crisis Detection

The system uses Azure OpenAI to analyze journal entries for signs of:
- Suicidal ideation or self-harm intentions
- Plans or methods to harm oneself or others
- Severe hopelessness or despair with no perceived way out
- Recent suicide attempts or severe self-harm
- Acute trauma or abuse

**Important:** The system is calibrated to avoid false positives. It does NOT flag:
- General sadness, stress, or anxiety
- Normal difficult emotions or everyday struggles
- Temporary feelings of being overwhelmed

### 2. Automatic Resource Display

When concerning content is detected:
- A modal appears immediately after entry submission
- Provides explanation of why resources are being shown
- Displays comprehensive crisis support contacts
- All resources are available 24/7

### 3. Manual Access to Resources

A **"🆘 Need Help Now?"** button is always visible in the app header:
- Accessible from any page
- No judgment - available for proactive support
- Same comprehensive resource list as automatic detection

## Crisis Resources Included

1. **988 Suicide & Crisis Lifeline**
   - Phone: 988
   - Text: 988
   - 24/7 free, confidential support

2. **Crisis Text Line**
   - Text: HOME to 741741
   - 24/7 support via text

3. **SAMHSA National Helpline**
   - Phone: 1-800-662-4357
   - Treatment referral and information

4. **Veterans Crisis Line**
   - Phone: 988 (Press 1)
   - Text: 838255
   - Support for veterans and service members

5. **The Trevor Project (LGBTQ Youth)**
   - Phone: 1-866-488-7386
   - Text: 678678
   - Crisis support for LGBTQ young people under 25

## Technical Implementation

### Backend Components

**File:** `MentalHealthJournal.Models/CrisisResource.cs`
- Defines crisis resource data model
- Static method `GetDefaultResources()` returns comprehensive resource list

**File:** `MentalHealthJournal.Models/JournalAnalysisResult.cs`
- Added fields:
  - `IsCrisisDetected` (bool)
  - `CrisisReason` (string?)
  - `CrisisResources` (List<CrisisResource>)

**File:** `MentalHealthJournal.Services/JournalAnalysisService.cs`
- New method: `DetectCrisisAsync()`
- Uses Azure OpenAI with JSON response format
- Low temperature (0.3) for consistent detection
- Fallback: returns false on error (doesn't false alarm)

### Frontend Components

**File:** `mentalhealthjournal.client/src/components/CrisisAlert.tsx`
- Modal component for displaying crisis resources
- Clean, accessible design
- Direct call, text, and web links
- Emphasizes emergency services (911) availability

**File:** `mentalhealthjournal.client/src/components/CrisisAlert.css`
- Responsive design for all screen sizes
- Attention-grabbing but not alarming styling
- Smooth animations for better UX
- Mobile-optimized contact buttons

**File:** `mentalhealthjournal.client/src/App.tsx`
- Integrates CrisisAlert component
- Handles automatic display when crisis detected
- "Need Help Now?" button in header with manual trigger
- Application Insights tracking for crisis detection events

## Privacy & Ethics

### Privacy Considerations
- Crisis detection happens server-side
- No additional data is stored beyond the journal entry
- Detection results are included in the API response but not persisted separately
- User privacy is maintained throughout the process

### Ethical Design
- **Non-judgmental:** Resources presented as supportive, not punitive
- **Accessible:** Always-available manual access removes stigma
- **Calibrated:** AI tuned to avoid excessive false positives
- **Comprehensive:** Multiple resource types (phone, text, web)
- **Inclusive:** Includes specialized resources for veterans and LGBTQ youth

### Limitations
- This is NOT a substitute for professional mental health care
- AI detection may not catch all crisis situations
- Users in immediate danger should call 911
- The system provides resources but cannot provide direct intervention

## Application Insights Tracking

The system tracks:
- `CrisisDetected` event when concerning content is identified
- Includes entry ID and reason in event properties
- `JournalEntrySubmitted` event includes `isCrisisDetected` flag
- Helps monitor system effectiveness and usage patterns

## Future Enhancements

Potential improvements:
1. **Multi-language support** for international crisis hotlines
2. **Geolocation-based resources** for local crisis services
3. **Follow-up reminders** to check in with users after crisis detection
4. **Optional wellness check-ins** for users who trigger detection
5. **Integration with emergency contacts** (with user permission)
6. **Severity levels** for different types of concerns

## Testing Recommendations

⚠️ **Important:** Be thoughtful when testing crisis detection with real content.

Safe testing approaches:
1. Use obvious test phrases: "This is a test of crisis detection"
2. Review detection logic in code rather than triggering with realistic content
3. Test manual "Need Help Now?" button functionality
4. Verify modal display and resource links work correctly

## Configuration

Crisis resources are currently hardcoded in:
- `MentalHealthJournal.Models/CrisisResource.cs` (backend)
- `mentalhealthjournal.client/src/App.tsx` (frontend manual trigger)

To customize resources:
1. Modify the `GetDefaultResources()` method
2. Add region-specific resources based on user location
3. Update frontend manual trigger resource list to match

## Deployment Notes

Before deploying:
1. ✅ Ensure Azure OpenAI deployment has sufficient quota
2. ✅ Test crisis detection with appropriate test cases
3. ✅ Verify all resource links are current and functional
4. ✅ Confirm Application Insights is tracking crisis events
5. ✅ Review and test on mobile devices for accessibility

## Support & Responsibility

**Reminder:** This feature is designed to provide helpful resources, but it cannot replace professional mental health services or emergency intervention. The application developers and operators should:

- Monitor Application Insights for crisis detection patterns
- Regularly update crisis resource contact information
- Consider legal/ethical implications in your jurisdiction
- Provide clear terms of service regarding limitations
- Consider consultation with mental health professionals for refinement

---

**For Immediate Crisis Support:**
- Call 988 (Suicide & Crisis Lifeline)
- Text HOME to 741741 (Crisis Text Line)
- Call 911 for emergencies
