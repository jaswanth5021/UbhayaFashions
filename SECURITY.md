# 🔒 Security Configuration Guide

## Overview

This document outlines the security measures implemented in Ubhaya Fashions and provides guidance for secure deployment.

## Security Fixes Applied (October 2026)

### ✅ Critical Fixes

1. **CORS Protection** - Changed from `AllowAnyOrigin()` to specific frontend URL
2. **HTTPS Enforcement** - Enabled HTTPS redirection and HSTS in production
3. **Secure Cookies** - Cookies now use Secure flag in production (HTTPS only)
4. **Secret Management** - Removed hardcoded secrets from Docker Compose

### ✅ Additional Improvements

5. **Language Classification** - Added `.gitattributes` to correctly classify as C# project
6. **Automated Updates** - Configured Dependabot for security patches
7. **Environment Variables** - Created `.env.example` template for secrets

---

## Production Deployment Checklist

### 🔴 CRITICAL - Before Going Live

- [ ] **Configure HTTPS/SSL Certificate**
  - AWS: Use Application Load Balancer with ACM certificate
  - Other: Use Let's Encrypt with Certbot
  - Cloudflare: Enable SSL/TLS encryption

- [ ] **Generate Strong Secrets**
  ```bash
  # Database password (32+ characters)
  openssl rand -base64 32
  
  # JWT secret key (64+ characters recommended)
  openssl rand -base64 48
  ```

- [ ] **Create .env File** (never commit this!)
  ```bash
  cp .env.example .env
  # Edit .env with your actual secrets
  ```

- [ ] **Set FrontendUrl** to your production domain
  ```env
  FRONTEND_URL=https://yourdomain.com
  ```

- [ ] **Configure Razorpay Production Keys**
  - Get from: https://dashboard.razorpay.com/app/keys
  - Use **live** keys, not test keys

### 🟠 HIGH PRIORITY

- [ ] **Enable Rate Limiting** (Add to Program.cs)
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.AddFixedWindowLimiter("login", opt =>
      {
          opt.Window = TimeSpan.FromMinutes(1);
          opt.PermitLimit = 5;
      });
  });
  ```

- [ ] **File Upload Validation** (Add to AdminProductsController.cs)
  ```csharp
  var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
  if (!allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
      return BadRequest("Invalid file type");
  if (file.Length > 10 * 1024 * 1024) // 10 MB
      return BadRequest("File too large");
  ```

- [ ] **Review Admin Access**
  - Change default admin email from `admin@ubhaya.com`
  - Use strong passwords for all admin accounts
  - Consider implementing 2FA

- [ ] **Database Backups**
  - Configure automated daily backups
  - Store backups in encrypted storage
  - Test restore procedure

### 🟡 MEDIUM PRIORITY

- [ ] **Add Security Headers** (Program.cs)
  ```csharp
  app.Use(async (context, next) =>
  {
      context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
      context.Response.Headers.Add("X-Frame-Options", "DENY");
      context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
      context.Response.Headers.Add("Referrer-Policy", "no-referrer-when-downgrade");
      await next();
  });
  ```

- [ ] **Enable Logging & Monitoring**
  - Configure Application Insights or Sentry
  - Set up alerts for errors and security events
  - Review logs weekly

- [ ] **Penetration Testing**
  - Use OWASP ZAP for automated scanning
  - Consider hiring security professionals
  - Fix all high/critical findings

---

## Environment Variables Reference

### Required for Production

| Variable | Purpose | Example |
|----------|---------|---------|
| `DB_PASSWORD` | SQL Server SA password | `<32+ char random>` |
| `JWT_SECRET_KEY` | JWT signing key | `<64+ char random>` |
| `FRONTEND_URL` | CORS allowed origin | `https://yourdomain.com` |
| `RAZORPAY_KEY_ID` | Payment gateway key | `rzp_live_...` |
| `RAZORPAY_KEY_SECRET` | Payment gateway secret | `<your-secret>` |

### Optional (but recommended)

| Variable | Purpose | Default |
|----------|---------|---------|
| `SMTP_HOST` | Email server | `smtp.gmail.com` |
| `SMTP_USERNAME` | Email account | - |
| `SMTP_PASSWORD` | Email app password | - |
| `DELHIVERY_API_KEY` | Shipping API | - |
| `GOOGLE_CLIENT_ID` | OAuth login | - |
| `FACEBOOK_APP_ID` | OAuth login | - |

---

## HTTPS Setup Guide

### Option 1: AWS Application Load Balancer

1. Create an Application Load Balancer in AWS Console
2. Request an SSL certificate in AWS Certificate Manager (ACM)
3. Configure ALB to use HTTPS listener on port 443
4. Point your domain to ALB DNS name
5. Update `FRONTEND_URL` in `.env`

### Option 2: Let's Encrypt (Certbot)

```bash
# Install Certbot
sudo snap install --classic certbot

# Get certificate (example with Nginx)
sudo certbot --nginx -d yourdomain.com

# Certificate auto-renews every 90 days
sudo certbot renew --dry-run
```

### Option 3: Cloudflare (Free SSL)

1. Add your domain to Cloudflare
2. Update nameservers at your domain registrar
3. Enable SSL/TLS in Cloudflare dashboard
4. Set SSL mode to "Full (strict)"
5. Enable "Always Use HTTPS"

---

## Testing Security

### 1. Verify HTTPS Enforcement

```bash
# Should redirect to HTTPS
curl -I http://yourdomain.com

# Should return 200 OK
curl -I https://yourdomain.com
```

### 2. Test CORS Protection

```bash
# Should reject requests from unauthorized origins
curl -H "Origin: https://malicious-site.com" \
     https://yourdomain.com/api/products
```

### 3. Check Cookie Security

- Open browser DevTools → Application → Cookies
- Verify cookies have `Secure` and `HttpOnly` flags in production

### 4. Scan for Vulnerabilities

```bash
# Run OWASP ZAP automated scan
docker run -t owasp/zap2docker-stable zap-baseline.py \
  -t https://yourdomain.com
```

---

## Incident Response

### If Secrets Are Exposed

1. **Immediately rotate all secrets:**
   - Generate new JWT key
   - Change database password
   - Regenerate Razorpay keys

2. **Invalidate existing sessions:**
   ```sql
   -- Force all users to re-login
   DELETE FROM AspNetUserTokens;
   ```

3. **Review access logs** for suspicious activity

4. **Notify affected users** if data breach occurred

---

## Security Contact

For security issues, please email: **security@ubhayafashions.com**  
Do NOT create public GitHub issues for security vulnerabilities.

---

## Regular Maintenance

### Weekly
- [ ] Review error logs for anomalies
- [ ] Check failed login attempts
- [ ] Monitor database size/performance

### Monthly
- [ ] Review admin accounts (disable unused)
- [ ] Update dependencies (Dependabot PRs)
- [ ] Test backup restoration

### Quarterly
- [ ] Security audit/penetration testing
- [ ] Review and rotate API keys
- [ ] Update disaster recovery plan

---

## Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Razorpay Security Guide](https://razorpay.com/docs/security/)
- [Let's Encrypt Documentation](https://letsencrypt.org/docs/)

---

**Last Updated:** October 24, 2026  
**Security Fixes Applied By:** Security Audit Bot
