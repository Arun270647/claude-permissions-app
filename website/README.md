# Claude Prompter - Website

> Modern, responsive landing page for Claude Prompter (formerly Claude Permission Assistant)

[![Deploy with Vercel](https://vercel.com/button)](https://vercel.com/new/clone?repository-url=https://github.com/Arun270647/cpa-web)
[![Live Site](https://img.shields.io/badge/live-cpa--web--swart.vercel.app-00F0FF)](https://cpa-web-swart.vercel.app/)

## 🌐 Live Site

**[cpa-web-swart.vercel.app](https://cpa-web-swart.vercel.app/)**

Deployed on Vercel with automatic deployments from the main branch.

## 📁 Repository Structure

```
cpa-web/
├── index.html              # Main landing page
├── assets/
│   ├── style.css          # Modern CSS with animations (1400+ lines)
│   └── script.js          # Interactive functionality
├── vercel.json            # Vercel deployment config (minimal)
├── VERCEL_DEPLOYMENT.md   # Deployment guide
├── WEBSITE_REDESIGN.md    # Design documentation
└── README.md              # This file
```

## ✨ Features

- 🎨 **Custom dark theme** with cyan (#00F0FF) and magenta (#FF3366) accents
- ⚡ **Animated backgrounds** - Rotating gradients and glassmorphism effects
- 📱 **Mobile responsive** - Works perfectly on all screen sizes
- 🖥️ **Auto-detecting downloads** - Detects Windows/macOS and architecture
- 🎭 **Terminal demo** - Live animated terminal showcasing the app
- 🎯 **Modern UI/UX** - Unique design, not AI-templaty!
- ⚡ **Smooth animations** - GPU-accelerated transitions
- ♿ **Accessible** - WCAG AA compliant

## What's Been Done

### v1.0.1 (Current)
✅ **Complete redesign** - Modern dark theme with custom animations  
✅ **Rebranded** from "Claude Permission Assistant" to "Claude Prompter"  
✅ **Vercel deployment** - Live at cpa-web-swart.vercel.app  
✅ **DMG downloads** for macOS - Professional .dmg packaging  
✅ **Auto OS detection** - Automatically suggests the right download  
✅ **Separate repository** - Clean separation from main app repo  
✅ **Direct downloads** - Fixed download handler to work with GitHub releases  
✅ **Responsive design** - Mobile-first, works on all devices  
✅ **Interactive elements** - Hover effects, smooth scrolling, animations  
✅ **Terminal demo** - Animated showcase of app functionality  

### Initial Version
✅ Basic landing page  
✅ Download links  
✅ Feature list  
✅ Documentation links  

## 🚀 Quick Deploy

### Deploy to Vercel (Recommended)

[![Deploy with Vercel](https://vercel.com/button)](https://vercel.com/new/clone?repository-url=https://github.com/Arun270647/cpa-web)

Or manually:

1. Go to [vercel.com/new](https://vercel.com/new)
2. Import this repository: `https://github.com/Arun270647/cpa-web`
3. Click "Deploy"
4. Done! ✅

**Auto-deployment is enabled** - Every push to `main` triggers a new deployment.

### Deploy to GitHub Pages

1. Go to repo **Settings** → **Pages**
2. Source: **Deploy from branch**
3. Branch: **main** → **/ (root)**
4. Save

Live at: `https://arun270647.github.io/cpa-web/`

### Deploy to Netlify

[![Deploy to Netlify](https://www.netlify.com/img/deploy/button.svg)](https://app.netlify.com/start/deploy?repository=https://github.com/Arun270647/cpa-web)

## 🛠️ Local Development

```bash
# Clone the repository
git clone https://github.com/Arun270647/cpa-web.git
cd cpa-web

# Serve locally (choose one):

# Option 1: Using Python
python -m http.server 8000

# Option 2: Using Node.js
npx serve .

# Option 3: Using PHP
php -S localhost:8000

# Option 4: Using Live Server (VS Code extension)
# Right-click index.html → Open with Live Server

# Visit: http://localhost:8000
```

## 📝 Customization

### Update Download URLs

Edit `assets/script.js`:

```javascript
const DOWNLOAD_URLS = {
    windows: 'https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.1/ClaudePrompter-Windows-v1.0.1.exe',
    macIntel: 'https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.1/ClaudePrompter-macOS-x64-v1.0.1.dmg',
    macArm: 'https://github.com/Arun270647/claude-permissions-app/releases/download/v1.0.1/ClaudePrompter-macOS-arm64-v1.0.1.dmg'
};
```

### Change Colors

Edit `assets/style.css`:

```css
:root {
    --color-primary: #00F0FF;      /* Cyan */
    --color-accent: #FF3366;       /* Magenta */
    --color-success: #00FF94;      /* Green */
    --color-bg: #0A0E14;          /* Dark background */
    --color-surface: #131821;     /* Card background */
}
```

### Add Analytics

Add to `<head>` in `index.html`:

```html
<!-- Google Analytics -->
<script async src="https://www.googletagmanager.com/gtag/js?id=G-XXXXXXXXXX"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'G-XXXXXXXXXX');
</script>
```

### Update Version Numbers

When releasing a new version, update in `assets/script.js`:

```javascript
const VERSION = '1.0.2'; // Update this
const DOWNLOAD_URLS = {
    windows: `https://github.com/.../v${VERSION}/ClaudePrompter-Windows-v${VERSION}.exe`,
    // ... update URLs
};
```

And in `index.html` (search for "v1.0.1" and replace).

## 📊 Tech Stack

- **HTML5** - Semantic markup
- **CSS3** - Modern features:
  - CSS Grid & Flexbox for layout
  - CSS Custom Properties (variables)
  - CSS Animations & Transitions
  - Backdrop filters for glassmorphism
  - Media queries for responsiveness
- **JavaScript ES6+** - Vanilla JS (no frameworks):
  - OS detection
  - Download handler
  - Smooth scrolling
  - Intersection Observer for animations
  - Code copy functionality
- **Google Fonts** - Inter (UI) & JetBrains Mono (code)
- **Vercel** - Deployment platform with CDN

## 🎨 Design Philosophy

**No generic templates.** No typical blue gradients. No "AI-generated looking" designs.

Just modern, unique design with personality.

**Design inspired by:**
- [Vercel](https://vercel.com) - Clean, modern aesthetics
- [Linear](https://linear.app) - Bold gradients, smooth animations
- [Raycast](https://raycast.com) - Professional yet playful
- [GitHub Copilot](https://github.com/features/copilot) - Developer-focused UX

**Key principles:**
1. **Dark-first** - Built for developers who code at night
2. **Animated but not distracting** - Subtle, purposeful motion
3. **Information density** - Show what matters, hide what doesn't
4. **Performance** - Fast load, GPU acceleration, no jank
5. **Unique** - Stand out from cookie-cutter landing pages

## 📖 Documentation

- [Deployment Guide](VERCEL_DEPLOYMENT.md) - Detailed deployment instructions
- [Design Documentation](WEBSITE_REDESIGN.md) - Design decisions and features
- [Main App Repository](https://github.com/Arun270647/claude-permissions-app) - Application source code
- [App Documentation](https://github.com/Arun270647/claude-permissions-app/tree/main/docs) - Technical docs

## 🔗 Related Links

- **Main App:** [claude-permissions-app](https://github.com/Arun270647/claude-permissions-app)
- **Latest Release:** [v1.0.1](https://github.com/Arun270647/claude-permissions-app/releases/tag/v1.0.1)
- **All Releases:** [Releases Page](https://github.com/Arun270647/claude-permissions-app/releases)
- **Report Issues:** [Issue Tracker](https://github.com/Arun270647/claude-permissions-app/issues)
- **Tech Stack:** [TECH_STACK.md](https://github.com/Arun270647/claude-permissions-app/blob/main/docs/TECH_STACK.md)

## 📄 License

MIT License - Same as main project

## 🤝 Contributing

This is the website repository. For application contributions, see the [main repository](https://github.com/Arun270647/claude-permissions-app).

### Website Improvements

1. Fork this repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Test locally (serve and check all pages/links)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Ideas for Contributions

- **Accessibility improvements** - Better screen reader support, keyboard navigation
- **Performance optimizations** - Reduce bundle size, optimize images
- **New sections** - Tutorials, case studies, video demos
- **Translations** - Multi-language support
- **Dark/Light toggle** - User preference for theme
- **Blog section** - Release notes, tutorials, tips
- **Search functionality** - Quick navigation to docs
- **Comparison table** - Claude Prompter vs manual approval

## 🎯 Performance

- ⚡ **Fast loading** - ~1.5s on 3G, <500ms on 4G
- 📦 **Minimal dependencies** - Just Google Fonts
- 🎨 **GPU-accelerated** - Smooth 60fps animations
- 📱 **Mobile-first** - Optimized for phones and tablets
- ♿ **Accessible** - WCAG AA compliant
- 🔍 **SEO optimized** - Meta tags, semantic HTML
- 📊 **Analytics ready** - Easy to add tracking

## 🌟 Features Showcase

### Hero Section
- Rotating gradient background
- Bold typography
- Dual-platform CTA buttons (Windows/Mac)
- Version badge

### Interactive Terminal Demo
- Animated typing effect
- Color-coded syntax
- Shows real Claude Code prompts
- Demonstrates auto-approval

### Features Grid
- Icon-based feature cards
- Hover effects with scale/glow
- Glassmorphism design
- Grid layout with responsive columns

### How It Works
- Step-by-step workflow
- Animated timeline
- Technical details
- Visual indicators

### Download Section
- Platform detection
- Auto-suggest correct download
- File size indicators
- Installation instructions

### FAQ Section
- Collapsible questions
- Smooth transitions
- Comprehensive answers
- Quick navigation

### Footer
- Social links
- Repository links
- Documentation links
- Version information

## 🚧 Roadmap

### Next Version
- [ ] **Blog section** - Release notes, tutorials, tips
- [ ] **Video demo** - Embedded YouTube/Vimeo walkthrough
- [ ] **Testimonials** - User feedback and success stories
- [ ] **Changelog page** - Detailed version history
- [ ] **Search functionality** - Quick doc navigation
- [ ] **Dark/Light toggle** - User theme preference
- [ ] **Multi-language support** - i18n for global users

### Future Enhancements
- [ ] **Interactive demo** - Try it in the browser (simulated)
- [ ] **Comparison table** - vs other automation tools
- [ ] **Performance dashboard** - Real-time stats (if API available)
- [ ] **Community showcase** - User projects and workflows
- [ ] **Newsletter signup** - Update notifications
- [ ] **Documentation search** - Algolia DocSearch integration
- [ ] **Code snippets** - Copy-paste setup commands
- [ ] **Version switcher** - View docs for different versions

## 📞 Support

**For website issues:**
- [Open an issue](https://github.com/Arun270647/cpa-web/issues)

**For app issues:**
- [Main repo issues](https://github.com/Arun270647/claude-permissions-app/issues)

**General questions:**
- Check the [FAQ section](https://cpa-web-swart.vercel.app/#faq)
- Read the [documentation](https://github.com/Arun270647/claude-permissions-app/tree/main/docs)

## 🙏 Acknowledgments

- **Design inspiration:** Vercel, Linear, Raycast, GitHub
- **Fonts:** Google Fonts (Inter, JetBrains Mono)
- **Hosting:** Vercel (amazing platform!)
- **Community:** All users who provide feedback and suggestions

---

<p align="center">
  Built with ❤️ for a smoother Claude Code experience
</p>

<p align="center">
  <a href="https://cpa-web-swart.vercel.app/">Website</a> •
  <a href="https://github.com/Arun270647/claude-permissions-app">Main App</a> •
  <a href="https://github.com/Arun270647/claude-permissions-app/releases">Download</a> •
  <a href="https://github.com/Arun270647/claude-permissions-app/blob/main/docs/TECH_STACK.md">Docs</a>
</p>

<p align="center">
  <strong>Claude Prompter v1.0.1</strong> - Your AI coding companion's best friend
</p>
