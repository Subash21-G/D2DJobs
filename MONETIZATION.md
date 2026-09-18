# Monetization setup

The portal includes category browsing, search, saved jobs, job detail pages and two responsive AdSense placements. `/Home/Advertise` accepts partnership enquiries through the existing business email. Ads are disabled by default; no dummy publisher code is sent to Google.

Add an `Advertising` section to your deployment's appsettings configuration (or use equivalent environment variables):

```json
"Advertising": {
  "Enabled": false,
  "SiteApproved": false,
  "ConsentConfigured": false,
  "PublisherId": "",
  "Slots": {
    "ListingTop": "",
    "ListingInline": "",
    "JobSidebar": ""
  }
}
```

1. Add your real `ca-pub-` publisher ID and the two ad unit slot IDs from your AdSense account. Environment variable example: `Advertising__PublisherId`; slot example: `Advertising__Slots__ListingInline`.
2. Publish the site and complete AdSense site review. `/ads.txt` automatically returns the Google seller entry once a valid publisher ID is configured; otherwise it returns 404.
3. Configure Google's required privacy/consent messages for your audience in your AdSense account, and review the site's privacy information before enabling ads.
4. Set `Advertising:Enabled`, `Advertising:SiteApproved` and `Advertising:ConsentConfigured` to true only when the setup described below is complete. Only configured placements render; empty search results, admin and policy pages do not load ad code. The inline slot appears only when there are at least five results.
5. Verify ads and ads.txt on the public domain. Approval, ad availability and revenue are controlled by Google and are not guaranteed by this code.

Reference: https://support.google.com/adsense/answer/12171612

Direct advertising enquiries work independently of AdSense. Confirm pricing and campaign details with advertisers manually; this update does not introduce checkout or automatically publish sponsored listings.

## Mobile and exploration update
Listings now show up to 12 actual jobs per page. A bottom exploration panel preserves filters when linking to the next page. Details offer related job cards and category/location routes; applications remain directly accessible. These links encourage exploration without forcing page views.

Only ListingInline (after four jobs, with at least five results) and JobSidebar (now below the complete detail/exploration content, on unexpired descriptions of at least 500 characters) are used. ListingTop has been removed to separate ads from filters. Placements are labeled Advertisement and have responsive reserved space. Text length is a conservative display heuristic, not an ad-policy compliance test.

Before ads can load, configure Advertising:SiteApproved=true and Advertising:ConsentConfigured=true in addition to Enabled, the publisher ID and actual slot IDs. Set these only after approval and an appropriate consent setup are complete. For EEA, UK and Swiss users, follow Google's certified CMP requirements. These flags record deployment readiness; they do not implement consent collection or guarantee compliance. Configure the CMP through the provider before switching them on. No live ads or invented IDs were enabled by this update.

Review the site's content, privacy disclosures and actual rendered ads before launch. Disable auto ads/anchor/vignette formats in AdSense if you want to retain only these deliberate placements. Never ask visitors to click ads or gate applications behind extra page views.

Policy references checked 17 September 2026:
- https://support.google.com/adsense/answer/1346295?hl=en
- https://support.google.com/adsense/answer/48182?hl=en
- https://www.google.com/about/company/user-consent-policy-help/
## Admin traffic and advertising reports
Open Admin Dashboard > Traffic & ad reports (/Analytics). Login is required.
- Page views: automatically counted public HTML GET requests in UTC, excluding logged-in admins. Counts start at deployment, include repeats/bots, and do not identify unique visitors. No analytics cookies or IP addresses are stored.
- Existing all-time job views and application clicks are displayed separately. These legacy counters can include admin traffic.
- AdSense impressions/clicks: manual daily entry from the official report, filtered to this website and UTC dates. Saving the same date replaces ad figures; page counts are preserved. Missing data says Not entered. Periods cover 7, 30 or 90 days including today; CTR uses only entered data.
- No automatic Google account connection, ad-click interception, revenue calculation or unique-visitor tracking is included. Future automatic synchronization requires authorized read-only AdSense API access.
- AddDailyAnalytics creates one aggregate database table through the existing startup migration process. Multi-instance page counting uses a database-locked atomic upsert. Tracking failures are logged without failing public requests.
- Local checks passed: migration/startup, login protection, missing antiforgery rejection, invalid report rejection, concurrent increments and responsive widths 320/390/1440. No fabricated ad report was saved. Local traffic figures include five requests from this test.
- Actual ad metrics reference: https://developers.google.com/adsense/management/reference/rest/v2/Metric
## Deployment correction - 18 September 2026
Production database migrations are explicit: generate and review artifacts/deploy.sql with scripts/Prepare-Release.ps1, then apply it through the database administration workflow. Automatic startup migrations run in Development or only when explicitly enabled. Admin > Launch readiness now reports pending migrations and advertising configuration; it cannot confirm Google account approval, live consent handling or actual ad delivery. See DEPLOYMENT.md.