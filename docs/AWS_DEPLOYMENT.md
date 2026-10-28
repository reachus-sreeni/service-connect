# Deploy Service Connect to AWS

This is a deployment runbook, not a claim that AWS resources exist. Requires an AWS account, permission to create resources, Docker, AWS CLI v2 and a domain if using a custom hostname. RDS, App Runner, networking and logs incur charges. Choose a region that supports the required services and an RDS SQL Server edition/version available in that region.

## Target architecture

Browser → CloudFront HTTPS → private S3 frontend origin (Origin Access Control)

CloudFront `/api/*` → App Runner HTTPS API → VPC connector → private RDS SQL Server

Use a dedicated demo environment. Choose an appropriate licensed RDS SQL Server edition (for example Express within its limits); the local container's Developer edition is not for production. Do not expose RDS publicly. Permit inbound TCP 1433 to RDS only from the VPC connector's security group and a controlled initialization host. Use RDS encryption and automated backups. Record the endpoint, database name and backup retention policy.

## 1. Build and push the API image to ECR

From the repository root, set your actual region/account and a unique release tag:

```sh
export AWS_REGION=us-east-1
export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export RELEASE_TAG=$(git rev-parse --short HEAD)
aws ecr create-repository --repository-name service-connect-api --region "$AWS_REGION"
aws ecr get-login-password --region "$AWS_REGION" | docker login --username AWS --password-stdin "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com"
docker buildx build --platform linux/amd64 --push -t "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/service-connect-api:$RELEASE_TAG" backend/ServiceConnect.Api
```

Skip repository creation if it already exists. The Docker build context must be `backend/ServiceConnect.Api`. App Runner pulls this immutable release tag. Retain the previous image for rollback.

## 2. Set secrets and initialize a fresh database

Create Secrets Manager secrets for a cryptographically random JWT key (at least 32 bytes) and the complete SQL connection string. Example connection-string shape:

```text
Server=YOUR_RDS_ENDPOINT,1433;Database=ServiceConnect;User Id=serviceconnect_app;Password=YOUR_SECRET;Encrypt=True;TrustServerCertificate=False
```

Use a dedicated SQL login. The initialization identity requires schema-creation privileges; the runtime login should have only the required SELECT/INSERT/UPDATE permissions on application tables. The application does not delete records. Do not put a master login in the runtime connection secret. Verify TLS with RDS's certificate chain/trust configuration rather than disabling certificate validation.

Run initialization from a temporary x64 host/container in the VPC (or a trusted host connected by VPN/tunnel) with access to RDS. Pull the same API image and inject private environment variables using your organization's secret delivery method. Then run:

```sh
docker run --rm --env ConnectionStrings__SqlServer --env Jwt__Key "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/service-connect-api:$RELEASE_TAG" --init-db
```

The variables must already be set securely in that shell. Avoid storing secrets in shell history or logs. The image's entrypoint passes `--init-db` to the app. This command applies the SQL Server EF migrations and seeds the service catalog. It does not seed demo accounts. Use a fresh database for this expanded schema; the earlier MVP used EnsureCreated without migration history, so an existing MVP database needs a reviewed baseline/import migration. See DATABASE.md. Apply future reviewed migrations as a separate release step. Bootstrap an administrator with --create-admin as documented there.

## 3. Create App Runner

In AWS Console → App Runner, create a service from the ECR image. Configure its ECR access role, port **8080**, and HTTP health path **/health**. Use manual deployment until verified.

Set these runtime variables:

| Name | Value/source |
| --- | --- |
| Jwt__Key | Secrets Manager ARN for JWT signing secret |
| ConnectionStrings__SqlServer | Secrets Manager ARN for SQL connection string |
| FrontendOrigin | Exact CloudFront/custom frontend origin, e.g. https://example.cloudfront.net |
| ASPNETCORE_ENVIRONMENT | Production |
| Database__Provider | SqlServer |
| Database__AutoInitialize | false |
| Database__SeedDemo | false |
| Verification__Delivery | External |
| Verification__Smtp__Host / Port / Username / Password / From | SMTP service settings; secret fields from Secrets Manager |
| Verification__Twilio__AccountSid / AuthToken / From | SMS service settings; AuthToken from Secrets Manager |

Grant the App Runner instance role `secretsmanager:GetSecretValue` for those specific secrets and `kms:Decrypt` if using a customer-managed KMS key. A reference to a secret is configured as a secret source, not as a plaintext value containing an ARN. Redeploy after rotating a referenced secret; existing instances do not automatically reload changed secret values.

Attach a VPC connector using private subnets and its dedicated security group. Confirm routing/DNS and security groups allow the database connection. Outbound application traffic uses the VPC connector; calls to public services need appropriate VPC egress (NAT/endpoints where applicable). App Runner inbound HTTPS remains public for CloudFront to reach; API authorization is enforced for requests. Restricting direct origin access can be added separately.

## 4. Create the frontend and HTTPS routing

```sh
cd frontend
npm ci
npm run build
```

Create a private S3 bucket with Block Public Access enabled. Upload the **contents** of `frontend/dist/service-connect/browser/` (run upload from the repository root):

```sh
aws s3 sync frontend/dist/service-connect/browser/ s3://YOUR_FRONTEND_BUCKET/ --delete
```

Create a CloudFront distribution with the S3 REST origin and Origin Access Control. Apply the generated bucket policy limiting reads to this distribution. Set default root object `index.html`; this application has no client-side path router, so no global 403/404 rewrite is required.

Add an HTTPS custom origin using the App Runner hostname. Create ordered behavior **`/api/*`** for that origin:

- Allow all HTTP methods, including POST and PUT. Redirect viewers to HTTPS.
- Use managed **CachingDisabled** policy. Never cache authenticated API responses.
- Use **AllViewerExceptHostHeader** origin-request policy so Authorization, Content-Type and viewer request data reach App Runner while Host matches the API origin.
- Keep S3 as the default behavior. Do not send API errors to index.html.

Use an ACM certificate in us-east-1 if adding a custom CloudFront domain, and point DNS at CloudFront. Update `FrontendOrigin` to the final frontend origin and redeploy the API. Invalidate `/index.html` after frontend releases; hashed bundles can have long cache lifetimes. The Angular development proxy is not used in production.

## 5. Verify before sharing

Check the App Runner `/health` URL, then visit CloudFront. Register customer/provider accounts with disposable email addresses, create a request, submit and revise an estimate, and accept the latest revision. Confirm another customer cannot see the request, another provider cannot see the quote, and edits/second acceptance return 409 after acceptance. Check CloudWatch for errors and verify database persistence after restarting the API.

`/health` reports process health only, so verify a database-backed operation as well. Run the full test suite with TEST_SQLSERVER against a disposable test server before promotion. Configure SMTP and Twilio and test contact verification with real delivery before inviting real users. Configure alarms for App Runner errors, RDS storage/connections and costs. Authentication throttling is local to each API instance and sees proxy IPs without trusted forwarded-header configuration; tune this before a multi-user launch.

## Rollback and cleanup

Deploy the previous ECR image tag and restore the previous S3 frontend release. Do not roll back schemas without a tested plan; back up before database changes. For cleanup, remove App Runner and its connector, CloudFront (disable first), S3 objects/bucket, ECR images/repository, unused VPC/NAT resources and secrets. Before removing RDS, decide whether to retain a final snapshot and check retained backup/snapshot charges.

## Official references

- https://docs.aws.amazon.com/apprunner/latest/dg/network-vpc.html
- https://docs.aws.amazon.com/apprunner/latest/dg/env-variable.html
- https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/using-managed-origin-request-policies.html
- https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-restricting-access-to-s3.html
- https://learn.microsoft.com/ef/core/providers/sql-server/
