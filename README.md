# OmniScan AR

**Spatial Knowledge Graph for Augmented Industrial Maintenance**

OmniScan AR is a cross-platform augmented/mixed reality system that lets industrial maintenance technicians point a smartphone or a Meta Quest 3 headset at a machine and instantly get spatially-anchored documentation, an interactive 3D digital twin, and a voice-driven Q&A assistant grounded in a knowledge graph of that equipment.

> Solo capstone project — built to demonstrate production-grade engineering across computer vision, spatial computing (AR/MR), graph databases, retrieval-augmented generation, and full-stack backend/frontend development.

[![Status](https://img.shields.io/badge/status-in%20development-yellow)]()
[![Phase](https://img.shields.io/badge/phase-1%20%2F%203-blue)]()
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## Why this project exists

Industrial maintenance technicians lose significant time searching through paper manuals and PDF documentation to find the information they need on the shop floor. OmniScan AR collapses that search into a glance: the camera recognizes the machine, relevant documentation and a 3D digital twin appear anchored in space around it, and the technician can simply ask a question out loud.

The same core client runs on a phone (accessible, fast to deploy at scale) and on a Meta Quest 3 (hands-free, immersive) — over 85% of the client code is shared between the two, which is the central architectural bet of this project 

## What it does

| # | Capability |
|---|---|
| O1 | Recognizes a machine automatically via smartphone camera or Quest 3 passthrough |
| O2 | Anchors information spatially in AR (mobile) or MR (headset) |
| O3 | Models object ↔ component ↔ documentation relationships in a knowledge graph |
| O4 | Answers spoken questions about the equipment via a RAG pipeline |
| O5 | Provides a single web back-office to manage data for every device |
| O6 | Ships as one Unity codebase, compiled for iOS/Android and Quest OS |
| O7 | Validates a flexible B2B product: mobile as the entry product, headset as the premium tier |
| O8 | Displays and manipulates an interactive 3D digital twin (touch on mobile, hand tracking on headset) |

## Project status

This project is planned and executed as three two-month phases, each broken into sprints and tracked as a GitHub Project. See the live board: **[OmniScan AR — Project Board](../../projects)**.


## Tech stack

| Layer | Technology |
|---|---|
| Client core (shared) | Unity 3D, C#, GLTFast |
| Mobile AR | Unity AR Foundation (ARCore / ARKit) |
| Quest 3 MR | Meta XR Core SDK (OpenXR), Passthrough, Hand Tracking |
| Computer vision | Python microservice — YOLOv8 |
| Backend & graph | Java Spring Boot + Neo4j |
| Generative AI | LLM + Vector DB (RAG pipeline) |
| Admin dashboard | Next.js + Tailwind CSS |
| DevOps | Docker + Docker Compose |

## Repository structure

```
omniscan-ar/
├── backend/                # Spring Boot API + Neo4j (Phase 1)
├── cv-service/             # Python YOLOv8 microservice (Phase 1)
├── dashboard/              # Next.js admin back-office (Phase 2)
├── unity-client/           # Unity project — mobile AR + Quest 3 MR (Phase 1–3)
└── .github/                # Issue templates, workflows
```

## Author

**Zakaria Guennani** — building this project to specialize in AR/MR/spatial computing, with the goal of moving into applied XR roles in industrial and health-tech contexts.


