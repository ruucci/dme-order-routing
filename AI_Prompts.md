create a webapi with the latest .net core LTS version - name it Synapse.OrderRouting.Api, also add test project for this api, containerize with docker
keep repos separate for csv handling - later want to be able to swap that out with db layer, keep architecture lightweight and testable
keep interfaces in separate folder
add swagger


debug why swagger not loading with http://localhost:8080/swagger

logic for order routing service - separate from controller, so business rules can be modified without changing the API


Validate order.
Load products from products.csv.
Load suppliers from suppliers.csv.
For each item, find eligible suppliers:
supplier supports the product category
if mail_order = false, supplier serves customer ZIP
if mail_order = true, supplier can mail order
Try to find one supplier that can fulfill all items.
If not possible, split items across best eligible suppliers.
Rank suppliers by:
fewer shipments first
higher satisfaction score
local over mail order when scores are close


set threshold for rating similarity ~1.0 points -> Prefer local if score difference <= 1.0, otherwise prefer higher score
add middleware for request logging, and exception handling, need to always return 200 OK for each request with feasible = true + routing / false + errors



Add Readme file for basics for running the application, extra  section for future considerations like (add any others that were missed):
future data layer to swap out static supplier and product data -> inventory, ratings, return rate, customer satisfaction around returns/refunds etc
caching around supplier zips, category, local/mail preference
normalize/clean up messy data first - not required on each routing


add docker instructions to Readme

Change naming for API from Synapse.OrderRouting to Dme.OrderRouting


add zip coverage service -> exact zip match, range match
merge duplicate product codes before routing, improve zip validation

cache suppliers and products in memory for faster processing - load at app start, and then api can keep using the cached data

add tests for all repos, and services
check coverage

add additional tests to up coverage for ZipCoverageService

delete unused files

add tests for middleware

remove extra null check, refactor to do these operations in a single lookup - foreach + linq is extra lookup, refactor where applicable


add coverlet settings to ignore app files and non-functional code coverage

add logging to ResolveItems, get eligible suppliers so we can report why an item was skipped - maybe useful for reporting later

validate both product code and category

move file names to configs for products and suppliers

make product parsing header-based like supplier, instead of 0 and 1 position in csv, will make it easier to add more columns later

add tests for these specific cases:
higher-rated supplier wins when both can fulfill
local supplier wins over mail-order when ratings are similar

debug failing tests, suggest fix


add gitignore file

customer_zip must be exactly 5 digits, add validation step, plus tests

results always show this zip range supplier, show possible fix-> ZIP range: 00100-99999

add another rule for broad range fulfillment rule >10k zips to be used last instead after local - i need to ignore this range or maybe treat it like mail only
make broadrange threshold configurable


third party zip lookup, radius/proximity calculator

convert to Azure durable Functions - longer running routing logic

reset all commits, soft

local changes still show 10k files

improve the readme structure and assumptions section

remove all broad range related configuration and rollback to original local and mail order fulfillment 
