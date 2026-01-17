# Bug Fix: User ID Consistency Issue

## Problem Identified

**Date**: January 16, 2026  
**Severity**: High  
**Impact**: Users may see older journal entries disappear after a few days

## Root Cause

The application had an inconsistency in how user IDs were being stored and retrieved:

1. **User Model** has two ID fields:
   - `id` - Cosmos DB document ID
   - `userId` - Partition key (intended to be the stable user identifier)

2. **JWT Token Generation** was incorrectly using `user.id` instead of `user.userId` for the `NameIdentifier` claim

3. **Username Availability Check** was comparing `existingUser.id` instead of `existingUser.userId`

### The Issue

When users logged in:
- Their JWT token would contain `user.id` as the `NameIdentifier`
- Journal entries were queried using this ID from the token
- If `user.id` ever differed from `user.userId`, the wrong entries would be retrieved
- Older entries created with a different token might become inaccessible

## Files Changed

### 1. `/MentalHealthJournal.Server/Controllers/AuthController.cs`
**Line 244**: Changed JWT claim generation
```csharp
// BEFORE
new Claim(ClaimTypes.NameIdentifier, user.id),

// AFTER
new Claim(ClaimTypes.NameIdentifier, user.userId), // Use userId (partition key) for consistency
```

### 2. `/MentalHealthJournal.Services/UserService.cs`
**Line 97**: Fixed username availability check
```csharp
// BEFORE
return existingUser == null || existingUser.id == currentUserId;

// AFTER  
return existingUser == null || existingUser.userId == currentUserId;
```

## Impact on Existing Users

### Good News
- **For new users**: No impact - they will work correctly from the start
- **For most existing users**: Both `id` and `userId` fields should be identical, so no data loss

### Potential Issues
If any users experienced the bug where their `id` and `userId` became mismatched:
- They may have journal entries associated with different user IDs
- After this fix, their NEW token will use the correct `userId`
- They should see all their entries correctly going forward

## Testing the Fix

### Manual Testing Steps

1. **Test Login Flow**
   ```bash
   # Login with Google
   # Verify JWT token contains correct userId
   # Create a journal entry
   # Logout and login again
   # Verify the entry is still visible
   ```

2. **Test Cross-Session Persistence**
   ```bash
   # Login and create entries on Day 1
   # Wait 24-48 hours
   # Login again and verify all entries are still visible
   ```

3. **Verify User ID Consistency**
   ```bash
   # Check Cosmos DB Users container
   # Verify id == userId for all users
   # If not, may need data migration
   ```

## Data Migration (If Needed)

If you find users in Cosmos DB where `id != userId`, you may need to run a migration:

```csharp
// Pseudocode for migration script
foreach (var user in allUsers)
{
    if (user.id != user.userId)
    {
        // Log the discrepancy
        Console.WriteLine($"Mismatch found: id={user.id}, userId={user.userId}");
        
        // Find all journal entries with this user.id
        var orphanedEntries = FindEntriesByUserId(user.id);
        
        // Update them to use user.userId
        foreach (var entry in orphanedEntries)
        {
            entry.userId = user.userId;
            await UpdateEntry(entry);
        }
        
        // Update chat sessions similarly
        var orphanedSessions = FindSessionsByUserId(user.id);
        foreach (var session in orphanedSessions)
        {
            session.userId = user.userId;
            await UpdateSession(session);
        }
    }
}
```

## Prevention

To prevent this issue in the future:

1. ✅ **Always use `userId`** as the stable user identifier
2. ✅ **Use `userId` as partition key** in all containers
3. ✅ **JWT tokens use `userId`** for NameIdentifier claim
4. ✅ **All queries filter by `userId`**

## Monitoring

After deployment, monitor:

1. **User Login Success Rate** - Should remain stable or improve
2. **Journal Entry Retrieval** - Users should see all their entries
3. **Application Insights Logs** - Check for any "User not found" errors
4. **User Feedback** - Monitor support requests about missing entries

## Rollback Plan

If issues occur after deployment:

1. Revert the two files changed
2. Deploy previous version
3. Investigate Cosmos DB data for id/userId mismatches
4. Run data migration if needed
5. Redeploy fix

## Additional Notes

- This bug likely affected users who:
  - Have been using the app for several days/weeks
  - Login frequently
  - May have experienced authentication issues in the past

- The fix ensures that going forward, all users will have consistent ID usage

- Recommend testing with a few pilot users before full deployment

---

## Next Steps

1. ✅ Code changes committed
2. ⏳ Build and test in development environment
3. ⏳ Verify existing user data in Cosmos DB
4. ⏳ Run data migration if needed
5. ⏳ Deploy to production
6. ⏳ Monitor for 48-72 hours
7. ⏳ Gather user feedback

**Status**: Ready for testing
