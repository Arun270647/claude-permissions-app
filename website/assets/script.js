// Download URLs - dynamically fetched from version.json or GitHub API
let DOWNLOAD_URLS = {
    windows: '',
    macIntel: '',
    macArm: '',
    releasesPage: 'https://github.com/Arun270647/claude-permissions-app/releases',
    version: 'loading...'
};

// Fetch version from local version.json (updated by release workflow)
async function fetchVersionInfo() {
    try {
        const response = await fetch('/version.json?nocache=' + Date.now());
        if (!response.ok) throw new Error('version.json not found');

        const data = await response.json();

        // Update DOWNLOAD_URLS from version.json
        DOWNLOAD_URLS = {
            windows: data.downloadUrls.windows,
            macIntel: data.downloadUrls.macX64,
            macArm: data.downloadUrls.macArm64,
            releasesPage: DOWNLOAD_URLS.releasesPage,
            version: data.version,
            releaseDate: data.releaseDate
        };

        // Update version badge in UI
        const versionBadge = document.getElementById('version-badge');
        if (versionBadge) {
            versionBadge.textContent = `v${data.version}`;
        }

        console.log(`✓ Version loaded: v${data.version} (${data.releaseDate})`);
        return true;
    } catch (error) {
        console.warn('Failed to fetch version.json, falling back to GitHub API:', error);
        return await fetchLatestReleaseFromGitHub();
    }
}

// Fetch latest release from GitHub API (fallback)
async function fetchLatestReleaseFromGitHub() {
    try {
        const response = await fetch('https://api.github.com/repos/Arun270647/claude-permissions-app/releases/latest');
        if (!response.ok) throw new Error('Failed to fetch releases');

        const release = await response.json();
        const version = release.tag_name.replace(/^v/, ''); // Remove 'v' prefix

        // Find assets
        const assets = release.assets;
        const windowsAsset = assets.find(a => a.name.includes('Windows') && a.name.endsWith('.exe'));
        const macArmAsset = assets.find(a => a.name.includes('arm64') && a.name.endsWith('.dmg'));
        const macX64Asset = assets.find(a => a.name.includes('x64') && a.name.endsWith('.dmg'));

        // Update DOWNLOAD_URLS
        DOWNLOAD_URLS = {
            windows: windowsAsset?.browser_download_url || DOWNLOAD_URLS.releasesPage,
            macIntel: macX64Asset?.browser_download_url || DOWNLOAD_URLS.releasesPage,
            macArm: macArmAsset?.browser_download_url || DOWNLOAD_URLS.releasesPage,
            releasesPage: DOWNLOAD_URLS.releasesPage,
            version: version
        };

        // Update version badge in UI
        const versionBadge = document.getElementById('version-badge');
        if (versionBadge) {
            versionBadge.textContent = `v${version}`;
        }

        console.log(`✓ Latest version from GitHub: v${version}`);
        return true;
    } catch (error) {
        console.error('Failed to fetch latest release from GitHub:', error);
        // Fallback to releases page if API fetch fails
        DOWNLOAD_URLS = {
            windows: 'https://github.com/Arun270647/claude-permissions-app/releases/latest',
            macIntel: 'https://github.com/Arun270647/claude-permissions-app/releases/latest',
            macArm: 'https://github.com/Arun270647/claude-permissions-app/releases/latest',
            releasesPage: 'https://github.com/Arun270647/claude-permissions-app/releases',
            version: 'latest'
        };
        return false;
    }
}

// Detect OS
function detectOS() {
    const userAgent = window.navigator.userAgent;
    const platform = window.navigator.platform;
    const macosPlatforms = ['Macintosh', 'MacIntel', 'MacPPC', 'Mac68K'];
    const windowsPlatforms = ['Win32', 'Win64', 'Windows', 'WinCE'];

    if (macosPlatforms.indexOf(platform) !== -1) {
        return 'macOS';
    } else if (windowsPlatforms.indexOf(platform) !== -1) {
        return 'Windows';
    }

    return 'other';
}

// Handle downloads
function handleDownload(platform) {
    let url;

    if (platform === 'Windows') {
        url = DOWNLOAD_URLS.windows;
    } else if (platform === 'macOS') {
        // Check for Apple Silicon vs Intel
        const isAppleSilicon = navigator.userAgent.includes('Mac') &&
                               navigator.platform === 'MacIntel' &&
                               navigator.maxTouchPoints > 1;
        if (isAppleSilicon) {
            url = DOWNLOAD_URLS.macArm;
        } else {
            url = DOWNLOAD_URLS.macIntel;
        }
    } else {
        // Fallback to GitHub releases page
        window.open('https://github.com/Arun270647/claude-permissions-app/releases/latest', '_blank');
        return;
    }

    // Navigate directly to the GitHub release URL - browser will download automatically
    if (url && url !== DOWNLOAD_URLS.releasesPage) {
        window.location.href = url;
    } else {
        // If URL is empty or fallback, open releases page
        window.open(DOWNLOAD_URLS.releasesPage, '_blank');
    }
}

// Initialize download buttons
function initializeDownloadButtons() {
    const os = detectOS();

    // Hero download buttons
    const downloadWindows = document.getElementById('downloadWindows');
    const downloadMac = document.getElementById('downloadMac');

    if (downloadWindows) {
        downloadWindows.addEventListener('click', () => handleDownload('Windows'));
    }

    if (downloadMac) {
        downloadMac.addEventListener('click', () => handleDownload('macOS'));
    }

    // Platform-specific download buttons
    const dlWin = document.getElementById('dlWin');
    if (dlWin) {
        dlWin.addEventListener('click', (e) => {
            e.preventDefault();
            handleDownload('Windows');
        });
    }

    const dlMac = document.getElementById('dlMac');
    if (dlMac) {
        dlMac.addEventListener('click', (e) => {
            e.preventDefault();
            handleDownload('macOS');
        });
    }

    // Final CTA button
    const finalCTA = document.getElementById('finalCTA');
    if (finalCTA) {
        finalCTA.addEventListener('click', () => handleDownload(os));
    }
}

// Smooth scroll for anchor links
function initializeSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

// Add scroll-triggered animations
function initializeScrollAnimations() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -100px 0px'
    });

    // Observe all feature cards
    document.querySelectorAll('.feature-card, .workflow-step, .platform-card').forEach(el => {
        observer.observe(el);
    });
}

// Add parallax effect to background
function initializeParallax() {
    const bgGradient = document.querySelector('.bg-gradient');

    if (bgGradient) {
        window.addEventListener('scroll', () => {
            const scrolled = window.pageYOffset;
            bgGradient.style.transform = `rotate(${scrolled * 0.02}deg)`;
        });
    }
}

// Add nav background on scroll
function initializeNavScroll() {
    const nav = document.querySelector('.nav');

    if (nav) {
        window.addEventListener('scroll', () => {
            if (window.scrollY > 100) {
                nav.style.background = 'rgba(10, 14, 20, 0.95)';
            } else {
                nav.style.background = 'rgba(10, 14, 20, 0.8)';
            }
        });
    }
}

// Copy code snippets
function initializeCodeCopy() {
    document.querySelectorAll('code').forEach(code => {
        code.style.cursor = 'pointer';
        code.title = 'Click to copy';

        code.addEventListener('click', () => {
            navigator.clipboard.writeText(code.textContent);

            // Visual feedback
            const originalText = code.textContent;
            code.textContent = 'Copied!';
            code.style.color = 'var(--color-success)';

            setTimeout(() => {
                code.textContent = originalText;
                code.style.color = '';
            }, 1000);
        });
    });
}

// Analytics (if needed later)
function trackDownload(platform) {
    // Add analytics tracking here
    console.log(`Download initiated: ${platform}`);
}

// Initialize everything when DOM is ready
document.addEventListener('DOMContentLoaded', async () => {
    // Fetch version info (version.json first, GitHub API fallback)
    await fetchVersionInfo();

    initializeDownloadButtons();
    initializeSmoothScroll();
    initializeScrollAnimations();
    initializeParallax();
    initializeNavScroll();
    initializeCodeCopy();

    console.log(`Claude Prompter website loaded 🚀 (v${DOWNLOAD_URLS.version})`);
});

// Add keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Ctrl/Cmd + K to focus search (if you add search later)
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        // Add search functionality here
    }
});

// Add Easter egg :)
let konamiCode = [];
const konamiSequence = ['ArrowUp', 'ArrowUp', 'ArrowDown', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'ArrowLeft', 'ArrowRight', 'b', 'a'];

document.addEventListener('keydown', (e) => {
    konamiCode.push(e.key);
    konamiCode = konamiCode.slice(-10);

    if (konamiCode.join('') === konamiSequence.join('')) {
        document.body.style.transform = 'rotate(180deg)';
        setTimeout(() => {
            document.body.style.transform = '';
        }, 2000);
    }
});
