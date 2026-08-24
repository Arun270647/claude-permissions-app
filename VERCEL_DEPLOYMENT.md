# Vercel Deployment Guide

Deploy your Claude Permission Assistant website to Vercel in minutes!

## ✅ Prerequisites

- GitHub account (you already have this)
- Vercel account (free - sign up with GitHub)

---

## 🚀 Deployment Steps

### Step 1: Sign Up for Vercel

1. Go to: **https://vercel.com/signup**
2. Click **"Continue with GitHub"**
3. Authorize Vercel to access your GitHub account

### Step 2: Import Your Repository

1. After signing in, you'll see the Vercel dashboard
2. Click **"Add New..."** → **"Project"**
3. Click **"Import Git Repository"**
4. Find `claude-permissions-app` in the list
5. Click **"Import"**

### Step 3: Configure Project

On the import screen:

**Framework Preset:** Select **"Other"** (it's a static HTML site)

**Root Directory:** 
- Click **"Edit"**
- Select `./` (root) - the default is correct

**Build Settings:**
- Leave everything as default
- No build command needed (static site)

**Git Branch:**
- Select **`web`** branch (important!)

**Environment Variables:**
- None needed

### Step 4: Deploy

1. Click **"Deploy"**
2. Wait 30-60 seconds for deployment
3. You'll see: ✅ **"Congratulations! Your project has been deployed"**

---

## 🌐 Your Live Website

After deployment, your site will be live at:

```
https://claude-permissions-app.vercel.app
```

Or a random URL like:
```
https://claude-permissions-app-xxxxx.vercel.app
```

**Copy this URL** - you'll add it to your README!

---

## 🎨 Custom Domain (Optional)

Want a custom domain like `claudehelper.com`?

### Option 1: Free Vercel Subdomain

In Vercel dashboard:
1. Go to your project
2. Click **"Settings"** → **"Domains"**
3. Add a custom subdomain: `your-name.vercel.app`

### Option 2: Your Own Domain

1. Buy a domain (Namecheap, Google Domains, etc.)
2. In Vercel dashboard → **Settings** → **Domains**
3. Click **"Add Domain"**
4. Enter your domain (e.g., `claudehelper.com`)
5. Follow Vercel's DNS setup instructions
6. Wait for DNS propagation (10-60 minutes)

---

## 🔄 Automatic Deployments

**Every time you push to the `web` branch, Vercel automatically rebuilds your site!**

Example workflow:
```bash
git checkout web
# Edit index.html or assets/style.css
git add .
git commit -m "Update website design"
git push origin web
# Vercel automatically deploys in ~30 seconds!
```

You can watch deployments at: https://vercel.com/dashboard

---

## 📝 Update README with Website Link

Once deployed, update your main README.md:

```bash
git checkout main

# Edit README.md - add this at the top after badges:
```

Add to README.md:
```markdown
**🌐 Website:** https://claude-permissions-app.vercel.app

**📦 Download:** [Get v1.0.0](https://github.com/Arun270647/claude-permissions-app/releases/tag/v1.0.0)
```

Then commit and push:
```bash
git add README.md
git commit -m "Add website link to README"
git push origin main
```

---

## 🔧 Vercel Configuration

The `vercel.json` file in the repository configures:

- ✅ Static file serving
- ✅ Asset caching (1 year for CSS/images)
- ✅ Proper routing for single-page site
- ✅ Optimized performance

No changes needed unless you want to customize!

---

## 📊 Vercel Dashboard Features

After deployment, you can:

- 📈 **Analytics** - See visitor stats (free tier)
- 🌍 **Domains** - Add custom domains
- 🔄 **Deployments** - View deployment history
- 📝 **Logs** - Check build and runtime logs
- ⚙️ **Settings** - Configure environment variables

---

## 🐛 Troubleshooting

### "Failed to load" error?

Make sure:
- You selected the **`web`** branch (not `main`)
- `index.html` exists in the root of the `web` branch
- `vercel.json` exists in the root

### Assets (CSS) not loading?

Check:
- `assets/style.css` path is correct in `index.html`
- The `assets/` folder exists in the `web` branch
- Clear browser cache (Ctrl+Shift+R)

### Wrong branch deployed?

1. Go to Vercel dashboard → your project
2. Click **Settings** → **Git**
3. Change **Production Branch** to `web`
4. Redeploy

### Want to preview before deploying?

Vercel automatically creates preview URLs for every commit. Check the **Deployments** tab to see all previews.

---

## 💡 Pro Tips

### 1. Preview Deployments

Every push to `web` creates a preview URL before going live. Check the Vercel dashboard to see previews.

### 2. Rollback if Needed

Made a mistake? 
1. Go to **Deployments** tab
2. Find a previous working deployment
3. Click **"..."** → **"Promote to Production"**

### 3. Add Vercel Badge to README

```markdown
[![Deployed on Vercel](https://vercel.com/button)](https://claude-permissions-app.vercel.app)
```

### 4. Environment Variables (for future)

If you add backend APIs later:
1. Go to **Settings** → **Environment Variables**
2. Add your API keys
3. They'll be available during build and runtime

---

## 🎯 Quick Commands Reference

```bash
# Make website changes
git checkout web
# ... edit files ...
git add .
git commit -m "Update website"
git push origin web
# Vercel auto-deploys!

# Check deployment status
# Go to: https://vercel.com/dashboard

# View live site
# Your URL: https://claude-permissions-app.vercel.app
```

---

## ✅ Checklist

After deployment:

- [ ] Website loads at Vercel URL
- [ ] Download button works
- [ ] All links point to correct GitHub pages
- [ ] Mobile view looks good
- [ ] Add website URL to main README.md
- [ ] Share the link! 🎉

---

## 📞 Need Help?

- **Vercel Docs:** https://vercel.com/docs
- **Vercel Support:** https://vercel.com/support
- **GitHub Issues:** https://github.com/Arun270647/claude-permissions-app/issues

---

**Ready to deploy?** Go to: **https://vercel.com/new**

Import your `claude-permissions-app` repository with the `web` branch and you're done! 🚀
