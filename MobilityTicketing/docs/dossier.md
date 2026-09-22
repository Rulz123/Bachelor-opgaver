# MobilityTicketing – Lecture 1 dossier

## System context

MobilityTicketing supports customers using public transport within a city.

Customers can search routes, purchase tickets and validate tickets when travelling. Transport operators maintain routes, stops and timetables. Operational staff need real-time availability, while business stakeholders need historical reporting.

The first implementation slice covers only operators, routes, stops, ordered route stops and scheduled trips.

## Access-pattern map

| Actor | Operation | Required data | Current slice |
|---|---|---|---|
| Customer | Search routes and upcoming trips | Routes, stops and trips | Supported |
| Customer | Purchase a ticket | User, product, trip, ticket and payment | Later |
| Customer | Validate a ticket | Ticket and validation | Later |
| Operator | Update routes and timetables | Operator, route, stops and trips | Partly supported |
| Operations | View real-time availability | Trip capacity and reservations | Later |
| Business | Produce historical reports | Tickets, payments and validations | Later |

## ER model

```mermaid
erDiagram
    OPERATORS ||--o{ ROUTES : operates
    ROUTES ||--o{ ROUTE_STOPS : contains
    STOPS ||--o{ ROUTE_STOPS : appears_at
    ROUTES ||--o{ TRIPS : schedules

    OPERATORS {
        text id PK
        text name
    }

    ROUTES {
        text id PK
        text operator_id FK
        text city_id
        text mode
        text short_name
    }

    STOPS {
        text id PK
        text city_id
        text name
    }

    ROUTE_STOPS {
        text route_id PK, FK
        integer stop_sequence PK
        text stop_id FK
    }

    TRIPS {
        text id PK
        text route_id FK
        date service_date
        timestamptz scheduled_departure_utc
        text status
    }
