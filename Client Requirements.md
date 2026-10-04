# Client Requirements

 This is a browser for digitized National Geographic issues. The Client is the sole intended consumer for the API (outside of test applications).

* The primary intended platform is an iPad.
  * Desktop webbrowser support is also needed, especially during development.
* Avoiding the standard Safari chrome is desired. With iPadOS, this seems best achieved with a PWA.
* The app is only intended to be used in a local network environment. The API will only be available inside of the network.
* There is no need for offline support.
* An OpenAPI spec of the API is located here: src/Api/Api.json

## Main Functional Areas

The app should present a homepage when loaded where the different functional areas of the app can be chosen.

### Global UI Requirements

A bar running across the top of the screen is desired. This would house breadcrumbs, current page context, etc. 
* At the top left should be a Home button/link/control that takes the user back to the homepage (this will probably the root of the breadcrumbs). 
* At the top right will be forward and backward buttons. Nearly every page of the application supports navigating forwards and backwards. When navigation is supported both buttons must be shown even if one direction is disabled.

### Browse by Decades

This allows a user to manually browse through issues organized by date. This is the primary focus of the app. Other functionality may also let the user navigate directly to certain issues/pages.

On the main page for this functionality the available decades should be shown.

Decades are shown with the MOST RECENT decade listed first. This is unlike most other pages, but is a deliberate choice because the later decades are far more interesting than the earlier ones.

Clicking on a decade takes you to the Decade view.

Breadcrumbs: Home > Decades
Navigation: None
API call: `/api/v1/decades/`

#### Decade
Displays all of the issues available in this decade, grouped by year (earliest first). 

Clicking on a year takes you to the Year view.
Clicking on an issue takes you directly to the Issue view.

Example Breadcrumbs: Home > Decades > 1980s
API call: `/api/v1/decades/{first year of the decade}/`
Navigation: Next/Previous decade (supplied by API)

#### Year
Displays all of the issues available in this year (earliest first).

Example Breadcrumbs: Home > Decades > 1980s > 1985
API call: `/api/v1/years/{year}/`
Navigation: Next/Previous year (supplied by API)

#### Issue
Displays information about an issue as well as previews of all pages.

The cover is displayed as a special case:
* A box at top should show the cover as a larger preview
  * The cover can be clicked like a normal page which will take you to the Page view for it.
* To the side of the cover is a table of contents with clickable links for each article. Clicking an article jumps you to the Page view for that page.

The remaining pages are shown with thumbnail previews. Associated with each page is a sort order and optionally a page number. Certain pages such as advertisements do not have a page number. The sort order and page number should be displayed.

No page number: "5"
Page number: "127 (Page 54)"

Example Breadcrumbs: Home > Decades > 1980s > 1985 > March 1
API call: `/api/v1/issues/{issue id}/`
Navigation: Next/Previous issue (supplied by API)

#### Page
Displays the full quality image for a page.
The image should be proportionally scaled to fit inside of the available space (IE it should not scroll by default).

Example Breadcrumbs: Home > Decades > 1980s > 1985 > March 1 > 124 (Page 23)
API call: `/api/v1/pages/{page id}/`
Navigation: Next/Previous page in the current issue (supplied by API). Navigation does NOT move to the next/previous issue.

### Search Articles

TODO

### Browse Ads

TODO

### Search Ads

TODO