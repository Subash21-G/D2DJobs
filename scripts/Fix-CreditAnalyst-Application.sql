-- Verified from Bank of Baroda's Corporate & Institutional Credit 2026/17
-- recruitment page on 2026-09-23. Only fills an empty application URL.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF (SELECT COUNT(*) FROM Jobs WHERE CompanyName = N'Bank of Baroda'
    AND Title = N'Credit Analyst - C&IC, MMG/S-II' AND YEAR(PostedDate) = 2026) <> 1
BEGIN
    ROLLBACK;
    THROW 50001, 'Expected exactly one matching 2026 listing. Review before updating.', 1;
END;
UPDATE Jobs SET ApplyLink = N'https://ibpsreg.ibps.in/bonwejul26/'
WHERE CompanyName = N'Bank of Baroda' AND Title = N'Credit Analyst - C&IC, MMG/S-II'
    AND YEAR(PostedDate) = 2026 AND (ApplyLink IS NULL OR LTRIM(RTRIM(ApplyLink)) = N'');
SELECT @@ROWCOUNT AS ApplicationLinksUpdated;
COMMIT;
