# V2_Genesis — City of Johannesburg Valuation Portal

V2_Genesis is the online portal of the **City of Johannesburg, Property Branch – Valuation Administration**.
Property owners, third parties and representatives use it to lodge and track valuation-roll
submissions; the admin team uses it to help clients and to find any submission.

## What the portal does

| Module | Clients | Admin team |
|---|---|---|
| **Objections** (GV 2023, Supplementary Rolls 1 – 4) | Lodge an objection while the roll is open, add evidence (48 hours), download acknowledgement and Section 49 notice | Lodge on behalf of a client, also after the client period (approved late objections) |
| **Appeals** | Lodge an appeal after the MVD notice (one per objection) | Lodge on behalf of a client (approved late condonation appeals) |
| **Section 51** | Owner receives a notice when a third party objects and can upload evidence in the Section 51 period | Same rules |
| **Section 78 Query / Review** | Lodge a query or review in the review period | Same rules |
| **Rebates** | Apply for rate rebates (pensioner, child-headed, disability, PBO …) | Capture for a client |
| **Property Attributes** | Check and submit property attributes, upload evidence | Review submissions, inspections, valuer evidence |
| **Notices** | Download Section 49 / 51 / 53 and outcome notices | Search and download for any client |

Clients sign in with an email and password. The admin team signs in with **Login with Windows**
(`JOBURG\` account, checked against the `UserManagement` database).

## Technology

- ASP.NET Core MVC on **.NET 10**
- SQL Server — EF Core (Identity, audit) and Dapper (roll data, stored procedures)
- ASP.NET Core Identity with claims; Windows / Negotiate authentication for admins
- QuestPDF for every PDF (forms, acknowledgements, notices)
- Serilog logging (`C:\Genesis Log`)
- Hosted on **IIS**

## Repository layout

```
V2_Genesis.slnx            solution
V2_Genesis/
  Controllers/             one controller per module (Objection, Section78, Rebates, Attributes, Admin, Notice …)
  Services/                business logic (Objection, Section51, Notice, Pdf, PropertySearch, Evidence, Admin …)
  Data/                    ApplicationDbContext (Identity + audit tables)
  Models/                  view models, results, settings
  Views/                   Razor views (client and admin layouts, forms, submission view)
  wwwroot/                 css, js (form validation, Section 6 rules, SA ID checks), images
  appsettings.json         settings for development
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
| `NoticeRolls` | Section 49 letter dates and financial years per roll |
| `Section51Rolls`, `ObjectionRolls` | Where Objection / Appeal Packs and Section 51 notices are saved |
| `Email` | SMTP server; `TestMode` sends all mail to one test address |
| `Section51:BypassDeadline` | Testing only — must be `false` in production |

## Business rules (do not break these)

- The admin team can always lodge objections and appeals (late objections / late condonation
  appeals). Clients never see the words CLO or CLA.
- The **48-hour evidence window**, the **Section 51 period** and the **Section 78 review period**
  apply to everyone, including the admin team.
- A **Third_Party** objection: no Section 49 notice in the e-mail or on the dashboard; the owner
  receives a Section 51 notice.
- Section 6: "Multiple Purposes" means MultiPurpose; a "Split – X" category cannot be combined
  with MultiPurpose or with X itself.
- POPIA: the owner's name is hidden from third parties and left blank on PDFs.
- Objection Pack: `{Data folder}\{Objection_No}`. Appeal Pack: `{Appeal folder}\{Appeal_No}`,
  with the objection pack zipped inside.

## Testing updates

There is no automated test project yet, so every update is tested by hand on UAT before it is merged.

1. **Branch** from the main branch: `git checkout -b test/<short-name>`.
2. **Build** with no new errors or warnings: `dotnet build`.
3. **Keep test mode on** for local / UAT runs: `Email:TestMode = true`
   (and, only if you must test outside a period, `Section51:BypassDeadline = true` — switch it back after).
4. **Test the flows your change touches**, at least:
   - [ ] Register / sign in as a client; Login with Windows as admin
   - [ ] Search a property and link it
   - [ ] Objection as **Owner** (acknowledgement e-mail + Section 49, pack folder created)
   - [ ] Objection as **Third_Party** (no Section 49; owner gets the Section 51 e-mail)
   - [ ] Add evidence inside and after the 48-hour window
   - [ ] Appeal (appeal pack contains the zipped objection pack)
   - [ ] Section 78 query / review, rebate application, attributes submission
   - [ ] View submission (all tabs, Section 6) and download the PDFs
   - [ ] Admin lodges on a closed roll; client screens show no CLO / CLA
   - [ ] Check the page on a phone-sized screen
5. **Check the log** in `C:\Genesis Log` for errors during your test.
6. **Write in the pull request** what changed, which flows you tested and screenshots of any screen
   that changed. Remove test data from UAT afterwards.

## Security

- Never commit passwords, connection strings or API keys. Use User Secrets locally and
  environment variables on the server (`Email__Password`, `ConnectionStrings__…`, `ReCaptcha__…`).
- User input reaches SQL only as parameters (Dapper `@params`, EF LINQ). Never build SQL by
  joining strings with user input.

## Contact

City of Johannesburg — Property Branch Data Section
2nd Floor Jorissen Place, 66 Jorissen St, Braamfontein · Tel 011 084 9823 · propertydata@joburg.org.za
