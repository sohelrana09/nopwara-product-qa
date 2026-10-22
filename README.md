# Nopwara Product Q&A for nopCommerce

Product questions and answers for nopCommerce, built with **Tailwind CSS** and **Alpine.js** for the Nopwara theme.

## Features

- **"Questions & answers" section on product pages:** answered questions show first, long lists collapse behind "Show all N questions", and the count appears in the heading.
- **Ask a question:** an inline form that posts over ajax.
  - It has antiforgery protection, the honeypot (`SecuritySettings.HoneypotEnabled`) and the captcha (same switch as product reviews: `CaptchaSettings.ShowOnProductReviewPage`).
  - Guests can be allowed to ask or sent to the login page.
- **Moderation in admin:** Catalog → Product questions.
  - Filter the grid by text and status (unanswered, answered, not published).
  - Bulk publish, unpublish and delete.
  - Edit a question to answer it.
  - Uses the existing product review permissions.
- **Emails** (Content management → Message templates):
  - `NopwaraQA.NewQuestion.StoreOwnerNotification` goes to the store owner.
  - `NopwaraQA.QuestionAnswered.CustomerNotification` goes to the customer once the answer is published, one time only.
  - Extra tokens: `%NopwaraQA.Question%`, `%NopwaraQA.Answer%`, `%NopwaraQA.CustomerName%`, plus the store, product and customer tokens.
- **SEO:** answered questions are output as `FAQPage` structured data (JSON-LD).
- **Data:** one table, `NopwaraProductQuestion`. It is created on install and dropped on uninstall.

## Requirements

- nopCommerce **5.00**
- A theme that renders the `nopwara_productdetails_after_tabs` widget zone in its product templates (Nopwara does):

```razor
@await Component.InvokeAsync(typeof(WidgetViewComponent), new { widgetZone = "nopwara_productdetails_after_tabs", additionalData = Model })
```

## Install

1. Copy this folder to `src/Plugins/`, add the project to `NopCommerce.sln` and build.
2. Install **Nopwara product Q&A** (Configuration → Local plugins) and restart. The widget is activated on install.
3. Add this line to the theme's Tailwind entry CSS, then rebuild the CSS:
   `@source "../../../../../Plugins/Nop.Plugin.Misc.NopwaraQA/Views";`

## Settings

| Setting | Default |
|---|---|
| Themes | `Nopwara` |
| Allow guests to ask | off |
| Questions must be approved | on |
| Show unanswered questions | off |
| Questions shown before "Show more" | 5 |
| Notify store owner | on |
| Notify customer | on |

## Overriding the markup

Copy `Views/ProductQuestions.cshtml` to `Themes/{YourTheme}/Views/Plugins/NopwaraQA/`.

## License

Copyright (C) 2026 Sohel Rana

Licensed under the [GNU Affero General Public License v3.0](LICENSE), the license nopCommerce itself is built on (the [nopCommerce Public License](https://www.nopcommerce.com/license) is AGPL v3 plus its additional terms). Parts of this project are adapted from nopCommerce source code. A store that uses it must keep nopCommerce's "Powered by nopCommerce" link, as the nopCommerce Public License requires.
