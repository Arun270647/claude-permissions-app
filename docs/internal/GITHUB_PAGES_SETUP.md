# GitHub Pages Setup Instructions

Your website is ready! Now let's deploy it to GitHub Pages.

## ✅ What's Done

- ✅ Website created (`website/index.html` + `website/assets/style.css`)
- ✅ Committed to `web` branch
- ✅ Pushed to GitHub

## 🚀 Enable GitHub Pages (3 Steps)

### Step 1: Go to Repository Settings

Open: https://github.com/Arun270647/claude-permissions-app/settings/pages

Or manually:
1. Go to your repository
2. Click "Settings" tab (top right)
3. Click "Pages" in left sidebar

### Step 2: Configure GitHub Pages

In the "Build and deployment" section:

1. **Source:** Select **"Deploy from a branch"**
2. **Branch:** 
   - Select **`web`** from dropdown
   - Select **`/website`** folder (or `/` if it asks for root or folder only)
   - Click **"Save"**

### Step 3: Wait for Deployment

- GitHub will start building your site
- Takes 1-2 minutes
- Refresh the page - you'll see a message like:
  ```
  Your site is live at https://arun270647.github.io/claude-permissions-app/
  ```

## 🌐 Your Website URL

After deployment completes, your website will be live at:

```
https://arun270647.github.io/claude-permissions-app/
```

## ✏️ Update README with Website Link

Once live, update the main README.md to add the website link:

```markdown
**Website:** https://arun270647.github.io/claude-permissions-app/
```

Add it near the top of README, maybe in the badges section or right after the description.

## 🔧 Troubleshooting

### "No source" or branch not showing?

Make sure:
- The `web` branch exists on GitHub (run `git push origin web`)
- The `website/` folder exists in the `web` branch
- Refresh the settings page

### 404 Error when visiting the site?

Two options:

**Option A:** Move files to root

```bash
git checkout web
mv website/* .
mv website/.* . 2>/dev/null
rmdir website
git add -A
git commit -m "Move website files to root for GitHub Pages"
git push origin web
```

Then in GitHub Pages settings, select **`/` (root)** instead of `/website`.

**Option B:** Add index redirect

Create `index.html` in root of `web` branch:

```html
<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="refresh" content="0; url=website/index.html">
</head>
<body>
    <p>Redirecting to <a href="website/index.html">website</a>...</p>
</body>
</html>
```

### Build fails?

Check the Actions tab: https://github.com/Arun270647/claude-permissions-app/actions

Look for errors in the workflow logs.

## 📝 Making Updates

To update the website in the future:

```bash
# Switch to web branch
git checkout web

# Make changes to website/index.html or website/assets/style.css
# ... edit files ...

# Commit and push
git add website/
git commit -m "Update website content"
git push origin web

# GitHub will automatically rebuild and redeploy
```

## 🎨 Custom Domain (Optional)

If you want a custom domain like `claude-permission-assistant.com`:

1. Buy a domain (Namecheap, Google Domains, etc.)
2. In GitHub Pages settings, add your custom domain
3. In your domain's DNS settings, add:
   - `A` record pointing to GitHub Pages IPs
   - `CNAME` record pointing to `arun270647.github.io`

See: https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site

---

## ✨ Website Features

Your landing page includes:

✅ **Hero section** with download buttons  
✅ **Before/After comparison** showing the problem it solves  
✅ **Features grid** with 6 key features  
✅ **Quick start guide** with 3 steps  
✅ **Tech stack badges**  
✅ **FAQ section** with common questions  
✅ **CTA (call to action)** with download link  
✅ **Mobile responsive** design  
✅ **Fast loading** (no frameworks, pure HTML/CSS)  

## 📊 After It's Live

Check:
- [ ] Website loads correctly
- [ ] Download button works
- [ ] All links point to correct GitHub pages
- [ ] Mobile view looks good (test on phone)
- [ ] Add website URL to README.md

---

**Next:** After GitHub Pages is live, update the main branch README to link to the website!
