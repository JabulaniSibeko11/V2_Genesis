# V2_Genesis — City of Johannesburg Valuation Portal

V2_Genesis is the online portal of the **City of Johannesburg, Property Branch – Valuation Administration**.
Property owners, third parties and representatives use it to lodge and track valuation-roll
submissions; the admin team uses it to help clients and to find any submission.

## What the portal does

| Module | Clients | Admin team |
|---|---|---|
| **Objections** (GV 2023, Supplementary Rolls 1 – 4) | Lodge an objection while the roll is open, add evidence (48 hours), download the acknowledgement and the Section 49 notice | Lodge on behalf of a client, also after the client period (approved late objections, CLO) |
| **Appeals** | Lodge an appeal after the Section 53 MVD notice (status `Notice-Sent`), one per objection, only inside the appeal period | Same rules — **no appeals after the appeal close date** |
| **Section 51** | The owner receives a notice when a third party objects and can upload evidence in the Section 51 period | Same rules; only the admin team can download the Section 51 notice of a third-party objection |
| **Section 53 MVD notice** | Download the Municipal Valuer's Decision (same layout as eNotice) | Search and download for any client |
| **Section 78 Query / Review** | Lodge a query or review; a property not found cannot be searched on LIS or lodged as omitted | Same rules |
| **Rebates** | Apply for rate rebates (pensioner, child-headed, disability, PBO …) | Capture for a client |
| **Property Attributes** | Submit attributes, upload evidence, book a physical inspection (secure link + PIN) | Review submissions, inspections, valuer evidence |
| **Notices** | Download Section 49 / 53 and outcome notices | Search and download for any client |

Clients sign in with an e-mail and password. The admin team signs in with **Login with Windows**
(`JOBURG\` account, checked against the `UserManagement` database). Roles: `Admin` (the shared
admin account) and `Client` (everyone else).

## Technology

- ASP.NET Core MVC on **.NET 10**
- SQL Server — EF Core (Identity, audit) and Dapper (roll data, stored procedures)
- ASP.NET Core Identity with claims; Windows / Negotiate authentication for admins
- QuestPDF for every PDF (forms, acknowledgements, notices) — Arial is registered at start-up
  (`Helpers/QuestPdfFonts.cs`), because QuestPDF 2026.9+ no longer uses system fonts by default
- Serilog logging (`C:\Genesis Log`)
- xUnit + Moq automated tests
- Hosted on **IIS**

## Repository layout

```
V2_Genesis.slnx            solution
V2_Genesis/
  Controllers/             one controller per module (Objection, Section78, Rebates, Attributes, Admin, Notice …)
  Services/                business logic (Objection, Section51, Notice, Pdf, PropertySearch, Evidence, Admin …)
    Objection/             appeal period rules, appeal dashboard data, pack folders
  Helpers/                 small pure helpers (status display, acknowledgement headings, number formats …)
  Data/                    ApplicationDbContext (Identity + audit tables)
  Models/                  view models, results, settings
  Views/                   Razor views (client and admin layouts, forms, submission view)
  wwwroot/                 css, js (form validation, Section 6 rules, SA ID checks), images
  appsettings.json         settings for development
V2_Genesis.Tests/          automated tests (xUnit + Moq)
  Helpers/                 status, headings, number formats, Section 6 rows, keys, dates
  Models/                  business rules on results (appeal eligibility, dashboard counts)
  Services/                appeal period, appeal dashboard data, Section 51, inspections, pack folders
```

## Run it locally

1. Install the **.NET 10 SDK** and Visual Studio 2022 / VS Code.
2. You need access to the UAT SQL Server databases:
   `Objection`, `Objection_Supp1`…`Objection_Supp4`, `Objection_Query`, `GenesisAttributes`, `UserManagement`.
3. Put your connection strings and keys in **User Secrets**, not in `appsettings.json`:
   ```bash
   cd V2_Genesis
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string>"
   dotnet user-secrets set "ReCaptcha:SecretKey" "<dev key>"
   ```
4. Run:
   ```bash
   dotnet run --project V2_Genesis
   ```
   The site opens on `https://localhost:7287`.

Keep `Email:TestMode = true` while developing — every e-mail then goes to `Email:TestRecipient`
instead of real clients.

> The app never creates or changes database tables by itself. Do **not** run
> `dotnet ef database update` against UAT or production.

## Configuration you will change most

All in `appsettings.json` (production values are set on the server):

| Setting | What it controls |
|---|---|
| `RollDates:<roll>` (`OpenDate`, `VisibleUntil`) | When clients can object on each roll |
| `ValuationRoll` | The current roll name and the open / close dates shown on the home page |
| `NoticeRolls` | Section 49 letter dates, financial years and roll titles per roll |
| `ObjectionRolls:<roll>` | `FileRootPath` (Objection Packs) and `AppealRootPath` (Appeal Packs) per roll |
| `Section51Rolls` | Where Section 51 notices are saved, postal address tables |
| `AppealMvd:TableName` | `Objection_MVD` (default) or `Objection_MVD1` — where the appeal dates are read |
| `Section53Pdf` | Optional: valuer name, contact line, portal link on the Section 53 notice |
| `Email` | SMTP server; `TestMode` sends all mail to one test address |
| `Section51:BypassDeadline` | Testing only — must be `false` in production |

Pack folders today:

| Roll | Objection Packs | Appeal Packs |
|---|---|---|
| Supp 3 | `C:\Sup3\Sup 3Objections\Sup3Data` | `C:\Sup3\Sup 3 Appeals\Sup3AppealData` |
| Supp 4 | `C:\Sup4\Sup 4Objections\Sup4Data` | `C:\Sup4\Sup 4 Appeals\Sup4AppealData` |

## Business rules (do not break these)

**Objections**
- The admin team can always lodge objections (late objections, CLO). Clients never see the words CLO or CLA.
- The **48-hour evidence window**, the **Section 51 period** and the **Section 78 review period**
  apply to everyone, including the admin team.
- Statuses: `Obj-Lodging` for 48 hours, then `Obj-Pending` (`App-Lodging` → `App-Pending`,
  `Que-Lodging` → `Query-Pending` / `Review-Pending`). The SQL Agent job **"Genesis status job"**
  saves the change; the dashboard already shows it when the 48 hours have passed. The evidence
  date is only shown while the status is Lodging.
- A **Third_Party** objection: status `Obj-Section51`, the owner receives a Section 51 notice and has
  30 days; the objector sees `Obj-Lodging` for 48 hours, then `Obj-Pending`. No Section 49 and no
  Section 51 download for the objector.
- Section 6: "Multiple Purposes" means MultiPurpose; a "Split – X" category cannot be combined
  with MultiPurpose or with X itself.
- POPIA: the owner's name is never shown to the client (`–` on the summary) and left blank on PDFs.

**Appeals**
1. The client receives the **Section 53 MVD notice**; the objection status becomes `Notice-Sent`
   (shown as *Finalised*).
2. **Lodge Appeal** shows only while today is inside the appeal period of that objection:
   `Appeal_Start_Date` – `Appeal_Close_Date` in `Objection_MVD` (the `*_ReviseMVD` dates when the MVD
   was revised). The dashboard shows *Appeal closes / Appeal closed / Appeal lodged* under the
   status. **The same rule applies to the admin team — there are no late appeals.**
3. CheckProperty → the client chooses the form and the appellant type → fills Sections 1 – 7.
   Section 6 "as decided" is filled with the **Municipal Valuer's Decision from `Obj_Property_Info`**
   (`New_*_MVD`, `New2_*_MVD`, `New3_*_MVD`, or the `*_ReviseMVD` values when `ReviseMVD` is set).
   `Objection_MVD` is only used for the appeal dates. Nothing else is copied from the objection.
4. On submit (one transaction): a row in `Obj_Property_Info_Appeal` (`App-Lodging`, `Obj_Ref` =
   objection number), and the sections in the normal `Obj_Section*` tables with
   `Appeal_Ref_S*` = `Appeal_ID` and `Objection_Ref_S*` = the appeal number. One appeal per objection.
5. The acknowledgement (*"{ROLL} APPEAL ACKNOWLEDGEMENT"*, details *"as listed in Municipal Valuer
   Decision"*, appeal period from `Objection_MVD`) and the appeal form are e-mailed to the addresses
   in the appeal's Section 1. The same Display page shows the summary.
6. **Appeal Pack**: `{AppealRootPath}\{Appeal_No}` — the zipped Objection Pack, the acknowledgement,
   the appeal form, `Submitted Evidence\` and `Representative\`.
7. No Section 51 notice is sent for an appeal (also not for a third-party appellant).

**Notices**
- Section 51 and Section 53 notices print a split only when it has values; the effective date is on
  the main row only.
- The Section 49 page on the site shows the notice without the signature (the signed notice is the PDF).

## Testing

Every update is tested in two ways: the **automated tests** must be green, and the
**flows you changed** are tested by hand on UAT.

### Automated tests (`V2_Genesis.Tests`)

The tests check the business rules without a database, e-mail or browser, so they run in a few
seconds (about 190 test cases).

| Test class | Rule it protects |
|---|---|
| `AppealEligibilityResultTests` | Appeal only after the MVD notice, inside the period, never twice; admin follows the same rule; messages never mention CLO / CLA |
| `AppealWindowRulesTests` | Open / closed / not-yet-open appeal period, revised MVD dates, merging the account's appeals |
| `AppealDashboardDataTests` | Only `Objection_MVD` / `Objection_MVD1` can be used; appeal rows never change objection rows |
| `AppealSection6RowsTests` | Appeal Section 6 = MVD main row + MVD splits in order, empty splits skipped |
| `AcknowledgementTextTests` | "{ROLL} APPEAL ACKNOWLEDGEMENT", "as listed in Municipal Valuer Decision", appeal period text |
| `NoticeNumberFormatTests` | Section 53 number formats ("R 29 184 000", "1 174") |
| `DashboardModelsTests` | Objection counts exclude appeal rows; Lodge Appeal only when the period is open |
| `StatusDisplayTests` | Lodging → Pending after 48 hours; unknown window keeps the stored status |
| `DashboardNoticeStatusHelperTests` | Which notice can be downloaded for each status |
| `Section51SplitTests` | No empty split blocks on the Section 51 notice |
| `InspectionValuerAccessTests` | Valuer details only for the owner, after the PIN, inside the PIN window; lock after 5 tries |
| `AttributeDashboardRulesTests` | A submission with an inspection appears only under Appointments |
| `ValuerPhotoResolverTests` | Valuer photo lookup |
| `WefDateFormatterTests` | The "With Effective Date" is written in full, day-first (10 January 2025) |
| `FloatKeyHelperTests` | Property keys never show as scientific notation |
| `PackFoldersTests` | Objection / Appeal Pack folder and zip names |

**Run them in Visual Studio:** *Test → Test Explorer → Run All* (Ctrl+R, A).
**Or from a terminal:**

```bash
dotnet test V2_Genesis.slnx
```

**Adding a test:** when you change a business rule or fix a bug, add a test that would have caught
it. Keep the rule in a small class without database code (see `Helpers/` and `Services/Objection/`)
so it can be tested, put the test in the folder that matches the app (`Helpers`, `Models`, `Services`)
and name it after the rule, e.g. `Admin_cannot_lodge_when_the_appeal_period_is_closed`. Code that
talks to SQL Server or SMTP is tested by replacing that part with Moq.

**Package versions:** keep the `Microsoft.AspNetCore.*` packages of the test project on the same
version as the app (*Manage NuGet Packages for Solution → Consolidate*).

### Testing an update by hand (UAT)

1. **Branch** from the main branch: `git checkout -b test/<short-name>`.
2. **Build and run the automated tests** — no new errors or warnings, all tests green.
3. **Keep test mode on** for local / UAT runs: `Email:TestMode = true`
   (and, only if you must test outside a period, `Section51:BypassDeadline = true` — switch it back after).
4. **Test the flows your change touches**, at least:
   - [ ] Register / sign in as a client; Login with Windows as admin
   - [ ] Search a property and link it; Refine Search
   - [ ] Objection as **Owner** (acknowledgement e-mail + Section 49, pack folder created)
   - [ ] Objection as **Third_Party** (no Section 49; owner gets the Section 51 e-mail; no Section 51 download for the objector)
   - [ ] Add evidence inside and after the 48-hour window; status Lodging → Pending
   - [ ] Appeal: Lodge Appeal only inside the period (client and admin), Section 6 shows the MVD,
         acknowledgement heading and appeal period, appeal pack in `{Roll} Appeals\…AppealData\{Appeal_No}`
   - [ ] View Appeal Form shows the appeal's own data
   - [ ] Section 78 query / review, rebate application, attributes submission and inspection link
   - [ ] View submission (all tabs, Section 6) and download the PDFs (Section 49 / 51 / 53)
   - [ ] Admin lodges an objection on a closed roll; client screens show no CLO / CLA
   - [ ] Check the page on a phone-sized screen
5. **Check the log** in `C:\Genesis Log` for errors during your test.
6. **Write in the pull request** what changed, which flows you tested, that the tests are green,
   and screenshots of any screen that changed. Remove test data from UAT afterwards.

## Security

- Never commit passwords, connection strings or API keys. Use User Secrets locally and
  environment variables on the server (`Email__Password`, `ConnectionStrings__…`, `ReCaptcha__…`).
- Production uses its own SQL login, never the development `sa` account.
- User input reaches SQL only as parameters (Dapper `@params`, EF LINQ). Never build SQL by
  joining strings with user input.
- The inspection link shows valuer details only to the signed-in owner after the PIN; every view
  is written to the audit trail.

## Contact

City of Johannesburg — Property Branch Data Section
2nd Floor Jorissen Place, 66 Jorissen St, Braamfontein · Tel 011 084 9823 · propertydata@joburg.org.za
