# 🎯 MatchSkill_Ai  
**AI-powered skill-to-job matching for the next generation workforce**  
*Built by Pioneers Team for #EduHackAI 2025*

---

## 🚀 One-Sentence Pitch  
MatchSkill_Ai instantly connects learners with real-world opportunities by matching their skills—**not just their résumés**—to the most relevant jobs, internships and micro-projects.

---

## 🧭 Problem  
- 75 % of entry-level applicants never hear back.  
- Employers drown in keyword-stuffed CVs.  
- Learners don’t know **which skills** actually unlock which roles.

---

## ✨ Solution  
A lightweight web app that  
1. Parses any profile (LinkedIn PDF, GitHub, or manual input).  
2. Extracts **hard & soft skills** with a fine-tuned LLM.  
3. Runs a semantic search against live job boards & internal databases.  
4. Returns **top 5 matches** with a “gap analysis” and learning path.

---

## 🛠️ Tech Stack  
| Layer        | Tools / Services                |
|--------------|---------------------------------|
| Front-end    | React + TailwindCSS             |
| Back-end     | Python (FastAPI)                |
| AI / NLP     | OpenAI GPT-4o mini, Sentence-BERT |
| Data Sources | LinkedIn Jobs API, GitHub REST  |
| Infra        | Docker, GitHub Actions, Azure   |

---

## 🎨 Visual Identity  

| Asset        | Preview / Link                  |
|--------------|---------------------------------|
| **Logo**     | ![Logo](./assets/logo-512.png)  |
| **Palette**  | `#0F1B4C` · `#4F7CFF` · `#00E5A0` |
| **Typography**| Inter (Headers) & JetBrains Mono (Code) |
| **Mascot**   | “PioBot” (friendly robot owl)   |

> All brand assets live in `/assets/branding`.

---

## 📦 Quick Start  

```bash
git clone https://github.com/<your-org>/MatchSkill_Ai.git
cd MatchSkill_Ai
cp .env.example .env          # add your keys
docker compose up --build
# App runs on http://localhost:3000
