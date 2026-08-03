#!/usr/bin/env bash
#
# seed-issues.sh
#
# Creates the "Update customer", "Add product — API and unit tests",
# "Delete customer — API and unit tests", and "Delete customer — acceptance
# tests" agent-task issues in this repository, following the format of
# .github/ISSUE_TEMPLATE/agent-task.yml. The delete work is split into a
# development issue (endpoint + unit tests) and a dependent automation issue
# (acceptance tests only), so the two concerns can be picked up and reviewed
# independently. The "Add product" issue is development-only (API + unit
# tests) by design; acceptance tests for it are left for a future issue.
#
# It also creates a "List customers" issue that intentionally does NOT follow
# the template — vague, unlabeled, and missing the context/success-criteria an
# agent needs to work autonomously. This is a deliberate example of a
# low-quality, off-template issue someone might add to the board directly, to
# demonstrate what NOT to do.
#
# Usage:
#   ./scripts/seed-issues.sh            # creates all five issues
#   ./scripts/seed-issues.sh --dry-run  # prints what would be created, no API calls
#
# Requirements:
#   - GitHub CLI (`gh`) installed and authenticated (`gh auth login`)
#   - Run from anywhere inside the repo (script resolves the repo automatically)

set -euo pipefail

DRY_RUN=false
if [[ "${1:-}" == "--dry-run" ]]; then
  DRY_RUN=true
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "Error: GitHub CLI ('gh') is not installed. Install it from https://cli.github.com/" >&2
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "Error: gh is not authenticated. Run 'gh auth login' first." >&2
  exit 1
fi

REPO="$(gh repo view --json nameWithOwner --jq .nameWithOwner)"
LABEL="agent-task"

echo "Repository: ${REPO}"

ensure_label() {
  if ! gh label list --repo "${REPO}" --json name --jq '.[].name' | grep -qx "${LABEL}"; then
    echo "Label '${LABEL}' not found — creating it."
    if [[ "${DRY_RUN}" == false ]]; then
      gh label create "${LABEL}" \
        --repo "${REPO}" \
        --description "Task defined for an AI coding agent to implement" \
        --color "5319E7"
    fi
  fi
}

create_issue() {
  local title="$1"
  local body="$2"

  echo "----------------------------------------"
  echo "Creating issue: ${title}"

  if [[ "${DRY_RUN}" == true ]]; then
    echo "[dry-run] Title: ${title}"
    echo "[dry-run] Body:"
    echo "${body}"
    LAST_ISSUE_NUMBER="<dry-run-issue-number>"
    return
  fi

  local url
  url="$(gh issue create \
    --repo "${REPO}" \
    --title "${title}" \
    --label "${LABEL}" \
    --body "${body}")"
  echo "Created: ${url}"
  LAST_ISSUE_NUMBER="${url##*/}"
}

ensure_label

# ---------------------------------------------------------------------------
# Issue 1: Update customer
# ---------------------------------------------------------------------------
read -r -d '' UPDATE_BODY <<'EOF' || true
### Context and inputs

- Related issue/PR: none — this is the next endpoint to add after `POST /customers` (Add) and `GET /customers/{id}` (Get)
- Starting point (branch, files, or module):
  - `backend/src/CustomerManagement.Api/Program.cs` — existing minimal API endpoints (`AddCustomer`, `GetCustomer`)
  - `backend/src/CustomerManagement.Api/Models/Customer.cs` and `Models/AddCustomerRequest.cs`
  - `backend/src/CustomerManagement.Api/Data/AppDbContext.cs`
  - `backend/tests/CustomerManagement.UnitTests/` (xUnit)
  - `backend/tests/CustomerManagement.AcceptanceTests/` (SpecFlow — `Features/`, `StepDefinitions/`, `Support/`)
- Constraints (frameworks, versions, style rules):
  - .NET minimal APIs (no controllers) — follow the existing endpoint style in `Program.cs`
  - EF Core with SQLite via `AppDbContext`
  - Reuse the existing `DataAnnotations` validation pattern (see `AddCustomerRequest`) for the update request body
  - Unit tests: xUnit, following the style/naming in `AddCustomerTests.cs` / `GetCustomerTests.cs`
  - Acceptance tests: SpecFlow Gherkin feature file + step definitions, following the style of `CreateCustomer.feature` / `GetCustomer.feature` and their step definitions
- Relevant background/context:
  - The API currently supports Add and Get only. Update and Delete are explicitly called out in the Swagger description as "Not included (yet)".
  - A customer has three editable fields: `firstName`, `lastName`, `email` (all required, `email` must be a valid email format).

### Expected outputs

- Plan: a written plan describing the approach, posted for review before/with the PR
- Pull request: implements `PUT /customers/{id}`, targets the correct branch, references this issue
- Evidence: `dotnet test` output for both `CustomerManagement.UnitTests` and `CustomerManagement.AcceptanceTests` showing all new and existing tests passing

### Success criteria

- Correct behaviour:
  - `PUT /customers/{id}` updates `firstName`, `lastName`, and `email` on an existing customer and returns `200 OK` with the updated customer record.
  - Returns `404 Not Found` if no customer with the given `id` exists.
  - Returns `400 Bad Request` with a validation problem if any field is missing or invalid (same validation approach as `POST /customers`).
  - Existing `POST /customers` and `GET /customers/{id}` behaviour is unchanged.
- Required checks: CI green (`dotnet test` for both test projects), no build warnings introduced
- Review/scan outcomes required: code review approval
- Explicitly NOT sufficient: "tests pass" alone — the PR must include both unit tests (success, not-found, validation-failure cases) AND acceptance tests (Gherkin scenarios exercising the endpoint end-to-end via `CustomerApiFactory`)

EOF

create_issue "[Agent Task]: Update customer" "${UPDATE_BODY}"

# ---------------------------------------------------------------------------
# Issue 1b: Add product — API and unit tests
# ---------------------------------------------------------------------------
read -r -d '' ADD_PRODUCT_BODY <<'EOF' || true
### Context and inputs

- Related issue/PR: none — this introduces a new `Product` entity and its first endpoint, following the same pattern used for `Customer`
- Starting point (branch, files, or module):
  - `backend/src/CustomerManagement.Api/Program.cs` — existing minimal API endpoints (`AddCustomer`, `GetCustomer`) as the style reference
  - `backend/src/CustomerManagement.Api/Models/Customer.cs` and `Models/AddCustomerRequest.cs` — reference for how entity/DTO pairs are structured and documented
  - `backend/src/CustomerManagement.Api/Data/AppDbContext.cs` — will need a new `DbSet<Product>`
  - `backend/tests/CustomerManagement.UnitTests/` (xUnit) — reference `AddCustomerTests.cs` for style/naming
- Constraints (frameworks, versions, style rules):
  - .NET minimal APIs (no controllers) — follow the existing endpoint style in `Program.cs`
  - EF Core with SQLite via `AppDbContext`; add a new EF Core migration for the `Product` table
  - Keep DTO and entity separate (`AddProductRequest` input DTO, `Product` entity/response), same as `AddCustomerRequest`/`Customer`
  - Use Data Annotations for validation on the request DTO (no duplicated validation logic in the handler)
  - Document the endpoint fully for Swagger (`.WithName()`, `.WithTags()`, `.WithSummary()`, `.WithDescription()`, `.Produces<T>()`, `.ProducesValidationProblem()`), and add XML doc comments to model properties
- Relevant background/context:
  - A product has three fields: `name` (string, required), `description` (string, optional), and `price` (decimal, required, must be greater than 0).
  - This issue is development only (endpoint implementation + unit tests + migration). Acceptance/automation tests are explicitly out of scope and will be handled by a separate follow-up issue.

### Expected outputs

- Plan: a written plan describing the approach, posted for review before/with the PR
- Pull request: implements `POST /products`, adds the `Product` model/entity, DbSet, and EF Core migration, targets the correct branch, references this issue
- Evidence: `dotnet test` output for `CustomerManagement.UnitTests` showing all new and existing tests passing

### Success criteria

- Correct behaviour:
  - `POST /products` creates a new product and returns `201 Created` with the created product record (including its generated `id`).
  - Returns `400 Bad Request` with a validation problem if `name` is missing, or `price` is missing or not greater than 0.
  - Existing `POST /customers`, `GET /customers/{id}` behaviour is unchanged.
- Required checks: CI green (`dotnet test` for `CustomerManagement.UnitTests`), no build warnings introduced
- Review/scan outcomes required: code review approval
- Explicitly NOT sufficient: "tests pass" alone — the PR must include unit tests covering the success case and each validation-failure case. Acceptance/automation tests are explicitly out of scope for this issue.

EOF

create_issue "[Agent Task]: Add product — API and unit tests" "${ADD_PRODUCT_BODY}"

# ---------------------------------------------------------------------------
# Issue 2a: Delete customer — API and unit tests
# ---------------------------------------------------------------------------
read -r -d '' DELETE_DEV_BODY <<'EOF' || true
### Context and inputs

- Related issue/PR: none — this is the next endpoint to add after `POST /customers` (Add) and `GET /customers/{id}` (Get)
- Starting point (branch, files, or module):
  - `backend/src/CustomerManagement.Api/Program.cs` — existing minimal API endpoints (`AddCustomer`, `GetCustomer`)
  - `backend/src/CustomerManagement.Api/Models/Customer.cs`
  - `backend/src/CustomerManagement.Api/Data/AppDbContext.cs`
  - `backend/tests/CustomerManagement.UnitTests/` (xUnit)
- Constraints (frameworks, versions, style rules):
  - .NET minimal APIs (no controllers) — follow the existing endpoint style in `Program.cs`
  - EF Core with SQLite via `AppDbContext`
  - Unit tests: xUnit, following the style/naming in `AddCustomerTests.cs` / `GetCustomerTests.cs`
- Relevant background/context:
  - The API currently supports Add and Get only. Update and Delete are explicitly called out in the Swagger description as "Not included (yet)".
  - This issue covers development only (the endpoint implementation and unit tests). Acceptance/automation tests are handled by a separate follow-up issue that depends on this one.

### Expected outputs

- Plan: a written plan describing the approach, posted for review before/with the PR
- Pull request: implements `DELETE /customers/{id}`, targets the correct branch, references this issue
- Evidence: `dotnet test` output for `CustomerManagement.UnitTests` showing all new and existing tests passing

### Success criteria

- Correct behaviour:
  - `DELETE /customers/{id}` removes the customer with the given `id` and returns `204 No Content` on success.
  - Returns `404 Not Found` if no customer with the given `id` exists.
  - A subsequent `GET /customers/{id}` for a deleted customer returns `404 Not Found`.
  - Existing `POST /customers` and `GET /customers/{id}` behaviour is unchanged.
- Required checks: CI green (`dotnet test` for `CustomerManagement.UnitTests`), no build warnings introduced
- Review/scan outcomes required: code review approval
- Explicitly NOT sufficient: "tests pass" alone — the PR must include unit tests (successful delete, not-found case). Acceptance/automation tests are explicitly out of scope for this issue.

EOF

create_issue "[Agent Task]: Delete customer — API and unit tests" "${DELETE_DEV_BODY}"
DELETE_DEV_ISSUE="${LAST_ISSUE_NUMBER}"

# ---------------------------------------------------------------------------
# Issue 2b: Delete customer — acceptance tests (depends on 2a)
# ---------------------------------------------------------------------------
read -r -d '' DELETE_AUTOMATION_BODY_TEMPLATE <<'EOF' || true
### Context and inputs

- Related issue/PR: depends on #__DEV_ISSUE__ — the `DELETE /customers/{id}` endpoint and its unit tests must be implemented and merged first
- Starting point (branch, files, or module):
  - `backend/tests/CustomerManagement.AcceptanceTests/` (SpecFlow — `Features/`, `StepDefinitions/`, `Support/`)
  - `backend/tests/CustomerManagement.AcceptanceTests/Features/GetCustomer.feature` and its step definitions, as a style reference
- Constraints (frameworks, versions, style rules):
  - SpecFlow Gherkin feature file + step definitions, following the style of `CreateCustomer.feature` / `GetCustomer.feature` and their step definitions
  - Exercise the API end-to-end via `CustomerApiFactory` (no mocking of the API layer)
- Relevant background/context:
  - This issue only covers automation/acceptance testing for delete. The `DELETE /customers/{id}` endpoint implementation and unit tests are delivered by issue #__DEV_ISSUE__ and must be complete before this work can start.

### Expected outputs

- Plan: a written plan describing the approach, posted for review before/with the PR
- Pull request: adds SpecFlow acceptance tests for `DELETE /customers/{id}`, targets the correct branch, references this issue
- Evidence: `dotnet test` output for `CustomerManagement.AcceptanceTests` showing all new and existing tests passing

### Success criteria

- Correct behaviour:
  - Acceptance tests cover: deleting an existing customer returns `204 No Content`; deleting a non-existent customer returns `404 Not Found`; a subsequent `GET /customers/{id}` for a deleted customer returns `404 Not Found`.
- Required checks: CI green (`dotnet test` for `CustomerManagement.AcceptanceTests`), no build warnings introduced
- Review/scan outcomes required: code review approval
- Explicitly NOT sufficient: reusing or duplicating unit tests — this issue must add true end-to-end SpecFlow scenarios exercising delete-then-get via `CustomerApiFactory`

EOF
DELETE_AUTOMATION_BODY="${DELETE_AUTOMATION_BODY_TEMPLATE//__DEV_ISSUE__/${DELETE_DEV_ISSUE}}"

create_issue "[Agent Task]: Delete customer — acceptance tests" "${DELETE_AUTOMATION_BODY}"


# ---------------------------------------------------------------------------
# Issue 3: List customers (intentionally off-template — no labels, no context,
# no success criteria, no boundaries. This is what a rushed, informal issue
# looks like when the agent-task template isn't followed.)
# ---------------------------------------------------------------------------
LIST_BODY="can we get an endpoint to list all the customers? shouldn't be a big deal, just add it whenever"

echo "----------------------------------------"
echo "Creating issue: List customers"

if [[ "${DRY_RUN}" == true ]]; then
  echo "[dry-run] Title: List customers"
  echo "[dry-run] Body:"
  echo "${LIST_BODY}"
  echo "[dry-run] Labels: (none — intentionally off-template)"
else
  url="$(gh issue create \
    --repo "${REPO}" \
    --title "List customers" \
    --body "${LIST_BODY}")"
  echo "Created: ${url}"
fi

echo "----------------------------------------"
echo "Done."
