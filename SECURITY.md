# Security policy

This repository is a portfolio / local-development project; it has not passed a production security assessment.

- Never commit `.env`, production databases, personal information, customer exports, credentials, private keys or backups.
- Use long, unique values for `ADMIN_PASSWORD` and `POSTGRES_PASSWORD`, and rotate compromised credentials.
- Do not run publicly with `ASPNETCORE_ENVIRONMENT=Development`.
- In production use TLS, a trusted reverse proxy, secure session cookies, fine-grained read permissions, monitoring, and a migration plan.
- Do not collect real customer data until KVKK/legal requirements are addressed.
- Manual payment records do not constitute a payment gateway or a fiscal ledger.

If you identify a security issue, contact the maintainer privately instead of sharing an exploit or real secrets in a public issue.
