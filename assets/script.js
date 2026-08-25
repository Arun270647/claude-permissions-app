// Download URLs
const DOWNLOAD_URLS = {
    windows: 'https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-Windows-x64-v1.0.0.exe',
    macIntel: 'https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-x64-v1.0.0',
    macArm: 'https://github.com/Arun270647/claude-permissions-app/releases/latest/download/ClaudePermissionAssistant-macOS-arm64-v1.0.0'
};

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
        url = isAppleSilicon ? DOWNLOAD_URLS.macArm : DOWNLOAD_URLS.macIntel;
    } else {
        // Fallback to GitHub releases page
        window.open('https://github.com/Arun270647/claude-permissions-app/releases/latest', '_blank');
        return;
    }

    window.location.href = url;
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
document.addEventListener('DOMContentLoaded', () => {
    initializeDownloadButtons();
    initializeSmoothScroll();
    initializeScrollAnimations();
    initializeParallax();
    initializeNavScroll();
    initializeCodeCopy();

    console.log('Claude Permission Assistant website loaded 🚀');
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
