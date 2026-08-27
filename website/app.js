/**
 * LOOTBUGS - INTERACTIVE GAME SHOWCASE JAVASCRIPT
 * Features:
 *  - Procedural 8-Legged Spider Robot IK (Inverse Kinematics) & Step Gait Simulator
 *  - Loot Drag & Speed Physical Calculator
 *  - Nest Shop & Upgrades Terminal with Exponential Progression
 *  - Hostile AI FSM Threat Radar
 *  - Web Audio API Procedural Sci-Fi Sound Synthesizer
 *  - Particle Canvas Background & Interactive UI
 */

document.addEventListener('DOMContentLoaded', () => {
  initAudio();
  initBackgroundParticles();
  initNavigation();
  initSpiderIKSimulator();
  initLootCalculator();
  initShopSimulator();
  initHostileAIRadar();
  initTerminalLog();
});

/* ==========================================================================
   1. WEB AUDIO API SYNTHESIZER (Pure Procedural SFX)
   ========================================================================== */
let audioCtx = null;
let soundEnabled = true;

function initAudio() {
  const toggleBtn = document.getElementById('sfx-toggle');
  if (!toggleBtn) return;

  toggleBtn.addEventListener('click', () => {
    soundEnabled = !soundEnabled;
    const status = toggleBtn.querySelector('.sound-status');
    const icon = toggleBtn.querySelector('.icon-sound');

    if (soundEnabled) {
      if (status) status.textContent = 'AUDIO ON';
      if (icon) icon.textContent = '🔊';
      playBeep(600, 0.08, 'sine');
    } else {
      if (status) status.textContent = 'MUTED';
      if (icon) icon.textContent = '🔇';
    }
  });
}

function getAudioContext() {
  if (!audioCtx) {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (AudioContext) {
      audioCtx = new AudioContext();
    }
  }
  if (audioCtx && audioCtx.state === 'suspended') {
    audioCtx.resume();
  }
  return audioCtx;
}

function playBeep(freq = 440, duration = 0.1, type = 'sine', gainVal = 0.15) {
  if (!soundEnabled) return;
  try {
    const ctx = getAudioContext();
    if (!ctx) return;

    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(freq, ctx.currentTime);
    
    gain.gain.setValueAtTime(gainVal, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);

    osc.connect(gain);
    gain.connect(ctx.destination);

    osc.start();
    osc.stop(ctx.currentTime + duration);
  } catch (e) {
    // Audio fail safe
  }
}

function playServoSound() {
  if (!soundEnabled) return;
  try {
    const ctx = getAudioContext();
    if (!ctx) return;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.type = 'triangle';
    osc.frequency.setValueAtTime(320, ctx.currentTime);
    osc.frequency.exponentialRampToValueAtTime(180, ctx.currentTime + 0.06);

    gain.gain.setValueAtTime(0.04, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.06);

    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.start();
    osc.stop(ctx.currentTime + 0.06);
  } catch (e) {}
}

function playCashChime() {
  if (!soundEnabled) return;
  try {
    const ctx = getAudioContext();
    if (!ctx) return;
    [523.25, 659.25, 783.99, 1046.50].forEach((freq, i) => {
      setTimeout(() => {
        playBeep(freq, 0.12, 'sine', 0.12);
      }, i * 50);
    });
  } catch (e) {}
}

function playAlertSiren() {
  if (!soundEnabled) return;
  try {
    const ctx = getAudioContext();
    if (!ctx) return;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.type = 'sawtooth';
    osc.frequency.setValueAtTime(800, ctx.currentTime);
    osc.frequency.linearRampToValueAtTime(300, ctx.currentTime + 0.25);

    gain.gain.setValueAtTime(0.12, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.25);

    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.start();
    osc.stop(ctx.currentTime + 0.25);
  } catch (e) {}
}

/* ==========================================================================
   2. BACKGROUND CYBER PARTICLES
   ========================================================================== */
function initBackgroundParticles() {
  const canvas = document.getElementById('bg-particle-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  let width, height;
  let particles = [];
  const particleCount = 45;

  function resize() {
    width = canvas.width = window.innerWidth;
    height = canvas.height = window.innerHeight;
  }
  window.addEventListener('resize', resize);
  resize();

  class Particle {
    constructor() {
      this.reset();
    }
    reset() {
      this.x = Math.random() * width;
      this.y = Math.random() * height;
      this.vx = (Math.random() - 0.5) * 0.4;
      this.vy = (Math.random() - 0.5) * 0.4;
      this.radius = Math.random() * 2 + 1;
      this.color = Math.random() > 0.4 ? 'rgba(9, 230, 215, ' : 'rgba(17, 75, 173, ';
      this.alpha = Math.random() * 0.4 + 0.1;
    }
    update() {
      this.x += this.vx;
      this.y += this.vy;
      if (this.x < 0 || this.x > width) this.vx *= -1;
      if (this.y < 0 || this.y > height) this.vy *= -1;
    }
    draw() {
      ctx.beginPath();
      ctx.arc(this.x, this.y, this.radius, 0, Math.PI * 2);
      ctx.fillStyle = `${this.color}${this.alpha})`;
      ctx.fill();
    }
  }

  for (let i = 0; i < particleCount; i++) {
    particles.push(new Particle());
  }

  function animate() {
    ctx.clearRect(0, 0, width, height);

    // Draw connecting lines
    for (let i = 0; i < particles.length; i++) {
      particles[i].update();
      particles[i].draw();

      for (let j = i + 1; j < particles.length; j++) {
        const dx = particles[i].x - particles[j].x;
        const dy = particles[i].y - particles[j].y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist < 130) {
          ctx.beginPath();
          ctx.moveTo(particles[i].x, particles[i].y);
          ctx.lineTo(particles[j].x, particles[j].y);
          ctx.strokeStyle = `rgba(9, 230, 215, ${0.12 * (1 - dist / 130)})`;
          ctx.lineWidth = 1;
          ctx.stroke();
        }
      }
    }
    requestAnimationFrame(animate);
  }
  animate();
}

/* ==========================================================================
   3. NAVIGATION & TABS
   ========================================================================== */
function initNavigation() {
  const mobileToggle = document.getElementById('mobileToggle');
  const navMenu = document.getElementById('navMenu');
  const navLinks = document.querySelectorAll('.nav-link');

  if (mobileToggle && navMenu) {
    mobileToggle.addEventListener('click', () => {
      navMenu.classList.toggle('open');
      playBeep(500, 0.05);
    });

    navLinks.forEach(link => {
      link.addEventListener('click', () => {
        navMenu.classList.remove('open');
      });
    });
  }

  // Interactive Lab Tabs
  const tabBtns = document.querySelectorAll('.lab-tab-btn');
  const tabContents = document.querySelectorAll('.lab-content');

  tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.getAttribute('data-tab');
      tabBtns.forEach(b => b.classList.remove('active'));
      tabContents.forEach(c => c.classList.remove('active'));

      btn.classList.add('active');
      const targetContent = document.getElementById(targetId);
      if (targetContent) {
        targetContent.classList.add('active');
      }
      playBeep(700, 0.06, 'sine');
    });
  });
}

/* ==========================================================================
   4. PROCEDURAL 8-LEGGED SPIDER ROBOT INVERSE KINEMATICS SIMULATOR
   ========================================================================== */
function initSpiderIKSimulator() {
  const canvas = document.getElementById('spiderCanvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');

  // Parameters
  let stepSpeedMult = 1.0;
  let legReachRadius = 55;
  let chassisColor = '#09e6d7';
  let flashlightOn = true;
  let obstaclesOn = true;

  // Obstacles
  const obstacles = [
    { x: 120, y: 100, w: 100, h: 40, label: 'SECTOR-A CRATE' },
    { x: 480, y: 80, w: 120, h: 45, label: 'PIPELINE JUNCTION' },
    { x: 220, y: 320, w: 140, h: 50, label: 'SUBSTATION TRANSFORMER' },
    { x: 500, y: 300, w: 90, h: 80, label: 'CONTAINMENT CELL' }
  ];

  // Spider Body State
  const spider = {
    x: 350,
    y: 230,
    targetX: 350,
    targetY: 230,
    angle: 0,
    radius: 24,
    speed: 3.5,
    legs: []
  };

  // 8 Leg angles relative to body orientation
  // Left side: -135°, -90°, -45°, -15° | Right side: +135°, +90°, +45°, +15°
  const legAngles = [
    -Math.PI * 0.75, // 0: Rear Left
    -Math.PI * 0.50, // 1: Mid-Rear Left
    -Math.PI * 0.25, // 2: Mid-Front Left
    -Math.PI * 0.08, // 3: Front Left
     Math.PI * 0.08, // 4: Front Right
     Math.PI * 0.25, // 5: Mid-Front Right
     Math.PI * 0.50, // 6: Mid-Rear Right
     Math.PI * 0.75  // 7: Rear Right
  ];

  // Initialize 8 legs
  for (let i = 0; i < 8; i++) {
    const angle = legAngles[i];
    const restX = spider.x + Math.cos(spider.angle + angle) * legReachRadius;
    const restY = spider.y + Math.sin(spider.angle + angle) * legReachRadius;

    spider.legs.push({
      index: i,
      mountAngle: angle,
      footX: restX,
      footY: restY,
      targetFootX: restX,
      targetFootY: restY,
      prevFootX: restX,
      prevFootY: restY,
      isStepping: false,
      stepProgress: 0,
      group: (i % 2 === 0) ? 0 : 1 // Alternating tripod groups
    });
  }

  // Stepping group timer
  let activeGaitGroup = 0;
  let gaitTimer = 0;

  // Mouse & Touch Interaction
  function updateTargetPosition(clientX, clientY) {
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    spider.targetX = (clientX - rect.left) * scaleX;
    spider.targetY = (clientY - rect.top) * scaleY;
  }

  canvas.addEventListener('mousemove', (e) => {
    updateTargetPosition(e.clientX, e.clientY);
  });

  canvas.addEventListener('touchmove', (e) => {
    e.preventDefault();
    if (e.touches.length > 0) {
      updateTargetPosition(e.touches[0].clientX, e.touches[0].clientY);
    }
  }, { passive: false });

  // UI Control Listeners
  const stepSpeedInput = document.getElementById('stepSpeed');
  const valStepSpeed = document.getElementById('val-stepSpeed');
  if (stepSpeedInput && valStepSpeed) {
    stepSpeedInput.addEventListener('input', (e) => {
      stepSpeedMult = parseFloat(e.target.value);
      valStepSpeed.textContent = `${stepSpeedMult.toFixed(1)}x`;
    });
  }

  const legReachInput = document.getElementById('legReach');
  const valLegReach = document.getElementById('val-legReach');
  if (legReachInput && valLegReach) {
    legReachInput.addEventListener('input', (e) => {
      legReachRadius = parseInt(e.target.value);
      valLegReach.textContent = `${legReachRadius}px`;
    });
  }

  const colorPills = document.querySelectorAll('.color-pill');
  colorPills.forEach(pill => {
    pill.addEventListener('click', () => {
      colorPills.forEach(p => p.classList.remove('active'));
      pill.classList.add('active');
      chassisColor = pill.getAttribute('data-color');
      playBeep(880, 0.05);
    });
  });

  const flashlightCheck = document.getElementById('flashlightToggle');
  if (flashlightCheck) {
    flashlightCheck.addEventListener('change', (e) => {
      flashlightOn = e.target.checked;
      playBeep(400, 0.05);
    });
  }

  const obstacleCheck = document.getElementById('obstacleToggle');
  if (obstacleCheck) {
    obstacleCheck.addEventListener('change', (e) => {
      obstaclesOn = e.target.checked;
      playBeep(450, 0.05);
    });
  }

  const btnResetSpider = document.getElementById('btn-reset-spider');
  if (btnResetSpider) {
    btnResetSpider.addEventListener('click', () => {
      spider.x = canvas.width / 2;
      spider.y = canvas.height / 2;
      spider.targetX = spider.x;
      spider.targetY = spider.y;
      spider.angle = 0;
      spider.legs.forEach(leg => {
        leg.footX = spider.x + Math.cos(leg.mountAngle) * legReachRadius;
        leg.footY = spider.y + Math.sin(leg.mountAngle) * legReachRadius;
        leg.targetFootX = leg.footX;
        leg.targetFootY = leg.footY;
        leg.isStepping = false;
      });
      playBeep(600, 0.08);
    });
  }

  const hudPos = document.getElementById('spider-pos');

  // Main Simulation Loop
  function updateSpider() {
    // 1. Smooth movement towards target
    const dx = spider.targetX - spider.x;
    const dy = spider.targetY - spider.y;
    const dist = Math.sqrt(dx * dx + dy * dy);

    if (dist > 3) {
      const targetAngle = Math.atan2(dy, dx);
      // Angle smoothing
      let diffAngle = targetAngle - spider.angle;
      while (diffAngle < -Math.PI) diffAngle += Math.PI * 2;
      while (diffAngle > Math.PI) diffAngle -= Math.PI * 2;
      spider.angle += diffAngle * 0.12 * stepSpeedMult;

      const moveStep = Math.min(dist * 0.08 * stepSpeedMult, spider.speed * stepSpeedMult);
      spider.x += Math.cos(spider.angle) * moveStep;
      spider.y += Math.sin(spider.angle) * moveStep;
    }

    if (hudPos) {
      hudPos.textContent = `POSITION: (${Math.round(spider.x)}, ${Math.round(spider.y)}) // ROTATION: ${Math.round(spider.angle * 180 / Math.PI)}°`;
    }

    // 2. Gait & Step Logic
    gaitTimer += 0.06 * stepSpeedMult;
    if (gaitTimer > 1.0) {
      gaitTimer = 0;
      activeGaitGroup = (activeGaitGroup === 0) ? 1 : 0;
    }

    spider.legs.forEach(leg => {
      // Calculate ideal rest position based on current body transform
      const idealX = spider.x + Math.cos(spider.angle + leg.mountAngle) * legReachRadius;
      const idealY = spider.y + Math.sin(spider.angle + leg.mountAngle) * legReachRadius;

      const footDistFromIdeal = Math.hypot(leg.footX - idealX, leg.footY - idealY);

      // Trigger step if foot is too far and leg belongs to active gait group
      if (!leg.isStepping && footDistFromIdeal > 22 && leg.group === activeGaitGroup) {
        leg.isStepping = true;
        leg.stepProgress = 0;
        leg.prevFootX = leg.footX;
        leg.prevFootY = leg.footY;
        
        // Predict forward momentum
        const leadDist = Math.min(dist, 30);
        leg.targetFootX = idealX + Math.cos(spider.angle) * (leadDist * 0.5);
        leg.targetFootY = idealY + Math.sin(spider.angle) * (leadDist * 0.5);

        playServoSound();
      }

      // Animate stepping leg along a smooth arc
      if (leg.isStepping) {
        leg.stepProgress += 0.14 * stepSpeedMult;
        if (leg.stepProgress >= 1.0) {
          leg.stepProgress = 1.0;
          leg.isStepping = false;
          leg.footX = leg.targetFootX;
          leg.footY = leg.targetFootY;
        } else {
          // Lerp position
          const t = leg.stepProgress;
          leg.footX = leg.prevFootX + (leg.targetFootX - leg.prevFootX) * t;
          leg.footY = leg.prevFootY + (leg.targetFootY - leg.prevFootY) * t;
        }
      }
    });
  }

  function drawSpider() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw grid background
    ctx.strokeStyle = 'rgba(9, 230, 215, 0.05)';
    ctx.lineWidth = 1;
    for (let x = 0; x < canvas.width; x += 30) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, canvas.height);
      ctx.stroke();
    }
    for (let y = 0; y < canvas.height; y += 30) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(canvas.width, y);
      ctx.stroke();
    }

    // Draw Obstacles
    if (obstaclesOn) {
      obstacles.forEach(obs => {
        ctx.fillStyle = 'rgba(18, 24, 38, 0.85)';
        ctx.fillRect(obs.x, obs.y, obs.w, obs.h);
        ctx.strokeStyle = 'rgba(9, 230, 215, 0.3)';
        ctx.strokeRect(obs.x, obs.y, obs.w, obs.h);

        ctx.fillStyle = 'rgba(9, 230, 215, 0.4)';
        ctx.font = '9px "JetBrains Mono"';
        ctx.fillText(obs.label, obs.x + 8, obs.y + 16);
      });
    }

    // Draw Volumetric Flashlight Spotlight
    if (flashlightOn) {
      ctx.save();
      ctx.translate(spider.x, spider.y);
      ctx.rotate(spider.angle);

      const grad = ctx.createRadialGradient(20, 0, 10, 180, 0, 180);
      grad.addColorStop(0, 'rgba(255, 255, 255, 0.4)');
      grad.addColorStop(0.3, 'rgba(9, 230, 215, 0.25)');
      grad.addColorStop(1, 'rgba(9, 230, 215, 0)');

      ctx.beginPath();
      ctx.moveTo(15, 0);
      ctx.arc(0, 0, 200, -Math.PI * 0.18, Math.PI * 0.18);
      ctx.closePath();
      ctx.fillStyle = grad;
      ctx.fill();
      ctx.restore();
    }

    // Draw 8 Procedural IK Legs
    spider.legs.forEach(leg => {
      const mountX = spider.x + Math.cos(spider.angle + leg.mountAngle) * (spider.radius * 0.75);
      const mountY = spider.y + Math.sin(spider.angle + leg.mountAngle) * (spider.radius * 0.75);

      // Height offset during step arc (sinusoidal elevation)
      const stepLift = leg.isStepping ? Math.sin(leg.stepProgress * Math.PI) * 14 : 0;

      // 2-Joint Inverse Kinematics calculation
      const dx = leg.footX - mountX;
      const dy = (leg.footY - stepLift) - mountY;
      const legDist = Math.hypot(dx, dy);

      const l1 = 28; // Femur length
      const l2 = 28; // Tibia length

      // Mid-joint calculation (Elbow / Knee pointing outward)
      const midAngle = Math.atan2(dy, dx) + (leg.index < 4 ? -0.5 : 0.5);
      const midX = mountX + Math.cos(midAngle) * (l1 * 0.9);
      const midY = mountY + Math.sin(midAngle) * (l1 * 0.9);

      // Draw Upper Joint (Femur)
      ctx.beginPath();
      ctx.moveTo(mountX, mountY);
      ctx.lineTo(midX, midY);
      ctx.strokeStyle = '#4a5568';
      ctx.lineWidth = 4;
      ctx.lineCap = 'round';
      ctx.stroke();

      // Draw Lower Joint (Tibia)
      ctx.beginPath();
      ctx.moveTo(midX, midY);
      ctx.lineTo(leg.footX, leg.footY - stepLift);
      ctx.strokeStyle = chassisColor;
      ctx.lineWidth = 2.5;
      ctx.stroke();

      // Draw Joint Pivot Dots
      ctx.beginPath();
      ctx.arc(midX, midY, 2.5, 0, Math.PI * 2);
      ctx.fillStyle = '#fff';
      ctx.fill();

      // Draw Foot Tip Contact
      ctx.beginPath();
      ctx.arc(leg.footX, leg.footY - stepLift, leg.isStepping ? 4 : 3, 0, Math.PI * 2);
      ctx.fillStyle = leg.isStepping ? '#ff3366' : chassisColor;
      ctx.fill();
    });

    // Draw Main Spherical Spider Body
    ctx.save();
    ctx.translate(spider.x, spider.y);
    ctx.rotate(spider.angle);

    // Shell shadow
    ctx.shadowColor = chassisColor;
    ctx.shadowBlur = 15;

    // Body Chassis
    ctx.beginPath();
    ctx.arc(0, 0, spider.radius, 0, Math.PI * 2);
    ctx.fillStyle = '#0e1422';
    ctx.fill();
    ctx.lineWidth = 2.5;
    ctx.strokeStyle = chassisColor;
    ctx.stroke();

    ctx.shadowBlur = 0;

    // Optical Eye Lenses (Glowing Cyan / Red)
    ctx.beginPath();
    ctx.arc(12, -7, 4, 0, Math.PI * 2);
    ctx.arc(12, 7, 4, 0, Math.PI * 2);
    ctx.fillStyle = chassisColor;
    ctx.fill();

    ctx.beginPath();
    ctx.arc(16, 0, 2.5, 0, Math.PI * 2);
    ctx.fillStyle = '#ffffff';
    ctx.fill();

    // Central core panel
    ctx.beginPath();
    ctx.arc(0, 0, 10, 0, Math.PI * 2);
    ctx.fillStyle = '#1a2333';
    ctx.fill();
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.2)';
    ctx.stroke();

    ctx.restore();
  }

  function renderLoop() {
    updateSpider();
    drawSpider();
    requestAnimationFrame(renderLoop);
  }
  renderLoop();
}

/* ==========================================================================
   5. LOOT DRAG & SPEED PHYSICS CALCULATOR
   ========================================================================== */
function initLootCalculator() {
  const BASE_SPEED = 8.0; // m/s
  const MIN_SPEED = 1.8;  // m/s
  
  let playerStrength = 1.0;
  let tetheredItems = [];

  const speedNeedle = document.getElementById('speedNeedle');
  const speedValueText = document.getElementById('speedValueText');
  const calcWeight = document.getElementById('calcWeight');
  const calcValue = document.getElementById('calcValue');
  const calcPenalty = document.getElementById('calcPenalty');
  const tetherCount = document.getElementById('tetherCount');
  const tetherItemsList = document.getElementById('tetherItemsList');
  const tetherPayload = document.getElementById('tetherPayload');
  const strengthSlider = document.getElementById('strengthSlider');
  const strengthVal = document.getElementById('strengthVal');
  const btnClearLoot = document.getElementById('btnClearLoot');

  function calculate() {
    const totalWeight = tetheredItems.reduce((sum, item) => sum + item.weight, 0);
    const totalCredits = tetheredItems.reduce((sum, item) => sum + item.val, 0);

    // Game formula: EffectiveSpeed = max(MinSpeed, BaseSpeed - (TotalWeight / PlayerStrength))
    const effectiveSpeed = Math.max(MIN_SPEED, BASE_SPEED - (totalWeight / playerStrength));
    const speedRatio = (effectiveSpeed - MIN_SPEED) / (BASE_SPEED - MIN_SPEED);
    
    // Needle rotation: -45deg to +135deg (180deg range)
    const angle = -45 + (speedRatio * 180);
    if (speedNeedle) {
      speedNeedle.style.transform = `rotate(${angle}deg)`;
    }

    if (speedValueText) speedValueText.textContent = effectiveSpeed.toFixed(1);
    if (calcWeight) calcWeight.textContent = `${totalWeight.toFixed(1)} kg`;
    if (calcValue) calcValue.textContent = `${totalCredits} C`;
    
    const penaltyPct = ((1 - (effectiveSpeed / BASE_SPEED)) * 100).toFixed(0);
    if (calcPenalty) calcPenalty.textContent = `-${penaltyPct}%`;

    if (tetherCount) tetherCount.textContent = tetheredItems.length;

    // Render list
    if (tetherItemsList) {
      if (tetheredItems.length === 0) {
        tetherItemsList.innerHTML = '<div class="empty-msg">No scrap attached. Click items above to tether them.</div>';
        if (tetherPayload) tetherPayload.textContent = 'NO ITEMS TETHERED';
      } else {
        tetherItemsList.innerHTML = tetheredItems.map((item, idx) => `
          <div class="tethered-chip">
            <span>${item.name} (${item.weight}kg &bull; ${item.val}C)</span>
            <button class="btn-remove-chip" data-index="${idx}">&times;</button>
          </div>
        `).join('');
        if (tetherPayload) {
          tetherPayload.textContent = `${tetheredItems.length} ITEMS // ${totalWeight.toFixed(1)}kg CARGO`;
        }
      }
    }
  }

  // Add Item Buttons
  document.querySelectorAll('.btn-loot-item').forEach(btn => {
    btn.addEventListener('click', () => {
      const name = btn.getAttribute('data-name');
      const weight = parseFloat(btn.getAttribute('data-weight'));
      const val = parseInt(btn.getAttribute('data-val'));
      tetheredItems.push({ name, weight, val });
      playBeep(580, 0.08, 'square', 0.1);
      calculate();
    });
  });

  // Remove Item
  if (tetherItemsList) {
    tetherItemsList.addEventListener('click', (e) => {
      if (e.target.classList.contains('btn-remove-chip')) {
        const idx = parseInt(e.target.getAttribute('data-index'));
        tetheredItems.splice(idx, 1);
        playBeep(350, 0.06);
        calculate();
      }
    });
  }

  // Clear All
  if (btnClearLoot) {
    btnClearLoot.addEventListener('click', () => {
      tetheredItems = [];
      playBeep(300, 0.08);
      calculate();
    });
  }

  // Strength Slider
  if (strengthSlider && strengthVal) {
    strengthSlider.addEventListener('input', (e) => {
      playerStrength = parseFloat(e.target.value);
      strengthVal.textContent = playerStrength.toFixed(2);
      calculate();
    });
  }

  calculate();
}

/* ==========================================================================
   6. SHOP & UPGRADES TERMINAL SIMULATOR
   ========================================================================== */
function initShopSimulator() {
  let playerCredits = 750;
  const maxLevels = { health: 5, speed: 5, strength: 5, stamina: 5, flashlight: 3 };
  const currentLevels = { health: 0, speed: 0, strength: 0, stamina: 0, flashlight: 0 };
  const baseCosts = { health: 50, speed: 60, strength: 75, stamina: 45, flashlight: 100 };

  const playerCreditsDisplay = document.getElementById('playerCredits');
  const btnDepositLoot = document.getElementById('btnDepositLoot');
  const btnResetShop = document.getElementById('btnResetShop');

  function updateShopUI() {
    if (playerCreditsDisplay) {
      playerCreditsDisplay.textContent = `${playerCredits} C`;
    }

    document.querySelectorAll('.shop-item-card').forEach(card => {
      const upgrade = card.getAttribute('data-upgrade');
      const level = currentLevels[upgrade];
      const maxLvl = maxLevels[upgrade];
      const baseCost = baseCosts[upgrade];

      // In-game formula: Cost = BaseCost * (1.20 ^ Level)
      const cost = Math.round(baseCost * Math.pow(1.20, level));

      const lvlNum = card.querySelector('.lvl-num');
      if (lvlNum) lvlNum.textContent = level;

      const meterBar = card.querySelector('.meter-bar');
      if (meterBar) meterBar.style.width = `${(level / maxLvl) * 100}%`;

      const buyBtn = card.querySelector('.btn-buy');
      const costTag = card.querySelector('.cost-tag');

      if (buyBtn && costTag) {
        if (level >= maxLvl) {
          buyBtn.disabled = true;
          buyBtn.querySelector('span:first-child').textContent = 'MAXED';
          costTag.textContent = '---';
        } else {
          costTag.textContent = `${cost} C`;
          buyBtn.disabled = playerCredits < cost;
          buyBtn.querySelector('span:first-child').textContent = 'BUY';
        }
      }
    });
  }

  // Buy Upgrades
  document.querySelectorAll('.btn-buy').forEach(btn => {
    btn.addEventListener('click', () => {
      const upgrade = btn.getAttribute('data-upgrade');
      const level = currentLevels[upgrade];
      const maxLvl = maxLevels[upgrade];
      const baseCost = baseCosts[upgrade];
      const cost = Math.round(baseCost * Math.pow(1.20, level));

      if (level < maxLvl && playerCredits >= cost) {
        playerCredits -= cost;
        currentLevels[upgrade]++;
        playCashChime();
        updateShopUI();
      }
    });
  });

  // Deposit Scrap Run Credits
  if (btnDepositLoot) {
    btnDepositLoot.addEventListener('click', () => {
      playerCredits += 250;
      playCashChime();
      updateShopUI();
    });
  }

  // Reset Upgrades
  if (btnResetShop) {
    btnResetShop.addEventListener('click', () => {
      Object.keys(currentLevels).forEach(k => currentLevels[k] = 0);
      playerCredits = 750;
      playBeep(400, 0.08);
      updateShopUI();
    });
  }

  updateShopUI();
}

/* ==========================================================================
   7. HOSTILE AI FSM THREAT RADAR
   ========================================================================== */
function initHostileAIRadar() {
  const monsterBlip = document.getElementById('monsterBlip');
  const monsterBlipLabel = document.getElementById('monsterBlipLabel');
  const sightCone = document.getElementById('sightCone');
  const statusChip = document.getElementById('fsm-status-chip');
  const distDisplay = document.getElementById('fsm-dist');

  const nodes = {
    idle: document.getElementById('node-idle'),
    patrol: document.getElementById('node-patrol'),
    chase: document.getElementById('node-chase'),
    attack: document.getElementById('node-attack')
  };

  let currentState = 'patrol';

  function setState(state) {
    currentState = state;

    // Reset nodes
    Object.values(nodes).forEach(n => n && n.classList.remove('active'));
    if (nodes[state]) nodes[state].classList.add('active');

    if (!statusChip) return;

    statusChip.className = 'status-chip';
    if (state === 'idle') {
      statusChip.classList.add('chip-patrol');
      statusChip.textContent = 'CURRENT FSM STATE: IDLE';
      if (distDisplay) distDisplay.textContent = 'DISTANCE TO TARGET: 24.0 m';
      if (monsterBlip) {
        monsterBlip.style.top = '25%';
        monsterBlip.style.left = '75%';
        monsterBlip.style.background = '#00ff66';
      }
      if (monsterBlipLabel) monsterBlipLabel.style.color = '#00ff66';
      if (sightCone) sightCone.style.transform = 'translateY(-50%) rotate(180deg)';
      playBeep(400, 0.08);
    } else if (state === 'patrol') {
      statusChip.classList.add('chip-patrol');
      statusChip.textContent = 'CURRENT FSM STATE: PATROL';
      if (distDisplay) distDisplay.textContent = 'DISTANCE TO TARGET: 18.4 m';
      if (monsterBlip) {
        monsterBlip.style.top = '30%';
        monsterBlip.style.left = '65%';
        monsterBlip.style.background = '#09e6d7';
      }
      if (monsterBlipLabel) monsterBlipLabel.style.color = '#09e6d7';
      if (sightCone) sightCone.style.transform = 'translateY(-50%) rotate(210deg)';
      playBeep(520, 0.08);
    } else if (state === 'chase') {
      statusChip.classList.add('chip-chase');
      statusChip.textContent = 'CURRENT FSM STATE: CHASE (SIGHTLINE CONFIRMED)';
      if (distDisplay) distDisplay.textContent = 'DISTANCE TO TARGET: 8.2 m [CLOSING]';
      if (monsterBlip) {
        monsterBlip.style.top = '42%';
        monsterBlip.style.left = '56%';
        monsterBlip.style.background = '#ff9e00';
      }
      if (monsterBlipLabel) monsterBlipLabel.style.color = '#ff9e00';
      if (sightCone) sightCone.style.transform = 'translateY(-50%) rotate(235deg)';
      playAlertSiren();
    } else if (state === 'attack') {
      statusChip.classList.add('chip-attack');
      statusChip.textContent = 'CURRENT FSM STATE: ATTACK (LETHAL ENGAGEMENT)';
      if (distDisplay) distDisplay.textContent = 'DISTANCE TO TARGET: 1.5 m [STRIKE]';
      if (monsterBlip) {
        monsterBlip.style.top = '48%';
        monsterBlip.style.left = '52%';
        monsterBlip.style.background = '#ff3366';
      }
      if (monsterBlipLabel) monsterBlipLabel.style.color = '#ff3366';
      if (sightCone) sightCone.style.transform = 'translateY(-50%) rotate(260deg)';
      playAlertSiren();
    }
  }

  const btnTriggerSight = document.getElementById('btnTriggerSight');
  const btnTriggerHide = document.getElementById('btnTriggerHide');
  const btnTriggerAttack = document.getElementById('btnTriggerAttack');

  if (btnTriggerSight) btnTriggerSight.addEventListener('click', () => setState('chase'));
  if (btnTriggerHide) btnTriggerHide.addEventListener('click', () => setState('patrol'));
  if (btnTriggerAttack) btnTriggerAttack.addEventListener('click', () => setState('attack'));
}

/* ==========================================================================
   8. CRT TERMINAL LOG SIMULATOR
   ========================================================================== */
function initTerminalLog() {
  const terminal = document.getElementById('terminalLog');
  if (!terminal) return;

  const dynamicLines = [
    { text: 'SCAVENGER SQUAD CONNECTED: [ALPHA-1, BETA-2, GAMMA-3]', color: 't-cyan' },
    { text: 'DUNGEON MAP SEED: #88921-GENERATE', color: 't-yellow' },
    { text: 'NAVMESH BAKING RUNTIME: 22ms COMPLETE.', color: 't-green' },
    { text: 'HOSTILE ENTITIES ACTIVE: 4 UNITS DETECTED.', color: 't-red' },
    { text: 'QUEEN TITHE TIMER RUNNING: 04:59 REMAINING.', color: 't-yellow' }
  ];

  let lineIdx = 0;
  setInterval(() => {
    if (lineIdx < dynamicLines.length) {
      const line = dynamicLines[lineIdx];
      const div = document.createElement('div');
      div.className = 'terminal-line';
      div.innerHTML = `<span class="prompt">&gt;</span> <span class="${line.color}">${line.text}</span>`;
      terminal.insertBefore(div, terminal.lastElementChild);
      lineIdx++;
    }
  }, 4000);
}
