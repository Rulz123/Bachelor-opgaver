workspace "Happy Headlines" "Architecture for the Happy Headlines platform." {

    model {
        publisher = person "Publisher" "Writes, saves, and publishes positive news articles."
        reader = person "Reader" "Reads articles, posts comments, and subscribes to the newsletter."
        observabilityPlatform = softwareSystem "Local Observability Stack" "Collects and stores logs, traces, and metrics locally using OpenTelemetry Collector, Jaeger, and Prometheus." "Docker Compose"

        happyHeadlines = softwareSystem "Happy Headlines" "Provides positive news, publishing, commenting, and newsletter functionality." {

            webapp = container "Webapp" "Allows publishers to create drafts and publish articles." "Web application"
            website = container "Website" "Displays articles and allows readers to comment and subscribe." "Web application"

            draftService = container "Draft Service" "Stores and retrieves article drafts with structured observability." ".NET 8 Minimal API"
            draftDatabase = container "Draft Database" "Stores article drafts." "PostgreSQL" "Database"
            centralLogging = container "Central Logging" "Collects structured JSON logs from all services." "OpenTelemetry Collector" "Observability"
            centralTracing = container "Central Tracing" "Collects and stores distributed traces." "OpenTelemetry Collector + Jaeger" "Observability"
            centralMetrics = container "Central Metrics" "Scrapes and queries service metrics." "OpenTelemetry Collector + Prometheus" "Observability"

            publisherService = container "Publisher Service" "Finalises approved articles for publication." "API"
            articleQueue = container "Article Queue" "Durable RabbitMQ queue with retry and dead-letter handling." "RabbitMQ" "Queue"
            newsletterService = container "Newsletter Service" "Consumes published article events idempotently." ".NET 8 Service"

            profanityService = container "Profanity Service" "Detects and manages prohibited language." ".NET 8 Minimal API"
            profanityDatabase = container "Profanity Database" "Stores configurable blocked words." "PostgreSQL" "Database"

            articleLoadBalancer = container "Article Service Load Balancer" "Routes REST requests across three Article Service instances." "NGINX"
            articleService1 = container "Article Service 1" "Stores and provides articles." ".NET 8 Minimal API"
            articleService2 = container "Article Service 2" "Stores and provides articles." ".NET 8 Minimal API"
            articleService3 = container "Article Service 3" "Stores and provides articles." ".NET 8 Minimal API"
            articleCache = container "ArticleCache" "Per-instance in-memory read cache; background refresh retains articles from the latest 14 days." ".NET in-memory cache"
            europeDatabase = container "Europe Article Database" "Stores Europe articles." "PostgreSQL" "Database"
            asiaDatabase = container "Asia Article Database" "Stores Asia articles." "PostgreSQL" "Database"
            africaDatabase = container "Africa Article Database" "Stores Africa articles." "PostgreSQL" "Database"
            northAmericaDatabase = container "North America Article Database" "Stores North America articles." "PostgreSQL" "Database"
            southAmericaDatabase = container "South America Article Database" "Stores South America articles." "PostgreSQL" "Database"
            australiaOceaniaDatabase = container "Australia/Oceania Article Database" "Stores Australia/Oceania articles." "PostgreSQL" "Database"
            antarcticaDatabase = container "Antarctica Article Database" "Stores Antarctica articles." "PostgreSQL" "Database"
            globalDatabase = container "Global Article Database" "Stores global articles." "PostgreSQL" "Database"

            commentService = container "Comment Service" "Stores comments only after profanity validation; isolates dependency failures with timeout, retries, and a circuit breaker." ".NET 8 Minimal API"
            commentDatabase = container "Comment Database" "Stores reader comments." "PostgreSQL" "Database"
            commentCache = container "CommentCache" "Per-instance read-through cache with miss population, write invalidation, and LRU eviction at 30 article entries." ".NET in-memory cache"

            subscriberService = container "Subscriber Service" "Manages newsletter subscriptions." "API"
            subscriberDatabase = container "Subscriber Database" "Stores subscriber information." "Database" "Database"
            subscriberQueue = container "Subscriber Queue" "Distributes new subscriber events." "Message queue" "Queue"

            newsletterDatabase = container "Newsletter Database" "Stores idempotent newsletter deliveries." "PostgreSQL" "Database"
            cacheDashboard = container "Cache Hit Ratio Dashboard" "Displays ArticleCache and CommentCache hit ratios." "Grafana dashboard"
        }

        publisher -> webapp "Creates drafts and publishes articles"
        reader -> website "Reads articles, comments, and subscribes"
        happyHeadlines -> observabilityPlatform "Exports structured logs, distributed traces, and metrics" "OTLP/gRPC"

        webapp -> draftService "Saves and retrieves drafts" "HTTPS/JSON"
        draftService -> draftDatabase "Stores and retrieves drafts"
        draftService -> centralLogging "Exports structured JSON logs" "OTLP/HTTP or gRPC"
        draftService -> centralTracing "Exports traces and metrics" "OTLP/gRPC"
        draftService -> centralMetrics "Exports metrics via OTLP" "OTLP/gRPC"
        centralMetrics -> cacheDashboard "Queries cache hit-ratio metrics from Prometheus" "PromQL"
        articleService1 -> centralLogging "Exports structured request logs" "OTLP"
        articleService2 -> centralLogging "Exports structured request logs" "OTLP"
        articleService3 -> centralLogging "Exports structured request logs" "OTLP"
        articleService1 -> centralTracing "Exports HTTP and queue consumer spans" "OTLP"
        articleService2 -> centralTracing "Exports HTTP and queue consumer spans" "OTLP"
        articleService3 -> centralTracing "Exports HTTP and queue consumer spans" "OTLP"
        commentService -> centralLogging "Exports structured request logs" "OTLP"
        profanityService -> centralLogging "Exports structured request logs" "OTLP"
        commentService -> centralTracing "Exports traces including outgoing calls" "OTLP"
        profanityService -> centralTracing "Exports server spans" "OTLP"
        articleService1 -> centralMetrics "Exports request metrics" "OTLP"
        articleService2 -> centralMetrics "Exports request metrics" "OTLP"
        articleService3 -> centralMetrics "Exports request metrics" "OTLP"
        commentService -> centralMetrics "Exports request metrics" "OTLP"
        profanityService -> centralMetrics "Exports request metrics" "OTLP"
        publisherService -> centralLogging "Exports structured request logs" "OTLP"
        newsletterService -> centralLogging "Exports structured request logs" "OTLP"
        publisherService -> centralTracing "Exports publishing and queue producer spans" "OTLP"
        newsletterService -> centralTracing "Exports queue consumer spans" "OTLP"
        publisherService -> centralMetrics "Exports request metrics" "OTLP"
        newsletterService -> centralMetrics "Exports request metrics" "OTLP"

        webapp -> publisherService "Publishes finished articles" "HTTPS/JSON"
        publisherService -> profanityService "Checks articles for prohibited language" "HTTPS/JSON"
        profanityService -> profanityDatabase "Retrieves and manages prohibited words"
        publisherService -> articleQueue "Publishes ArticlePublished events with traceparent headers" "RabbitMQ"

        articleLoadBalancer -> articleService1 "Balances REST requests" "HTTP"
        articleLoadBalancer -> articleService2 "Balances REST requests" "HTTP"
        articleLoadBalancer -> articleService3 "Balances REST requests" "HTTP"
        articleService1 -> europeDatabase "Routes Europe articles"
        articleService1 -> asiaDatabase "Routes Asia articles"
        articleService1 -> africaDatabase "Routes Africa articles"
        articleService1 -> northAmericaDatabase "Routes North America articles"
        articleService1 -> southAmericaDatabase "Routes South America articles"
        articleService1 -> australiaOceaniaDatabase "Routes Australia/Oceania articles"
        articleService1 -> antarcticaDatabase "Routes Antarctica articles"
        articleService1 -> globalDatabase "Routes global articles"
        articleService2 -> europeDatabase "Uses the same z-axis partitions"
        articleService3 -> europeDatabase "Uses the same z-axis partitions"
        articleService1 -> articleCache "Uses recent and single-article reads; writes update the cache"
        articleService2 -> articleCache "Uses its per-instance cache"
        articleService3 -> articleCache "Uses its per-instance cache"
        articleCache -> europeDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> asiaDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> africaDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> northAmericaDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> southAmericaDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> australiaOceaniaDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> antarcticaDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> globalDatabase "Background refresh reads articles from the latest 14 days"
        articleCache -> centralMetrics "Exports hit, miss, and hit-ratio metrics" "OTLP"
        articleQueue -> articleService1 "Delivers events; consumer continues trace from message headers" "RabbitMQ"
        articleQueue -> articleService2 "Delivers events; consumer continues trace from message headers" "RabbitMQ"
        articleQueue -> articleService3 "Delivers events; consumer continues trace from message headers" "RabbitMQ"
        articleQueue -> newsletterService "Delivers events; consumer continues trace from message headers" "RabbitMQ"
        newsletterService -> newsletterDatabase "Stores idempotent newsletter deliveries"
        website -> articleLoadBalancer "Retrieves recent and highlighted articles" "HTTP/JSON"

        website -> commentService "Posts and retrieves comments" "HTTPS/JSON"
        commentService -> profanityService "Checks comments directly; bounded timeout and retries" "HTTP/JSON"
        commentService -> profanityService "Opens circuit after repeated failures; fails closed with 503" "Fault-isolated swimlane"
        commentService -> commentCache "Reads by article; misses populate cache and LRU retains the 30 most recently accessed article IDs"
        commentCache -> commentDatabase "Loads comments on cache miss"
        commentService -> commentDatabase "Persists writes then invalidates affected cached article comments"
        commentCache -> centralMetrics "Exports hit, miss, and hit-ratio metrics" "OTLP"

        website -> subscriberService "Creates newsletter subscriptions" "HTTPS/JSON"
        subscriberService -> subscriberDatabase "Stores and retrieves subscribers"
        subscriberService -> subscriberQueue "Places new subscriber events on the queue"
        newsletterService -> subscriberQueue "Subscribes to new subscriber events"

        newsletterService -> articleLoadBalancer "Retrieves recent articles" "HTTP/JSON"
        newsletterService -> subscriberService "Retrieves active subscribers" "HTTPS/JSON"
    }

    views {
        systemContext happyHeadlines "SystemContext" {
            include *
            autoLayout lr
        }

        container happyHeadlines "Containers" {
            include *
            autoLayout tb
        }

        styles {
            element "Person" {
                shape Person
                background #08427b
                color #ffffff
            }

            element "Software System" {
                background #1168bd
                color #ffffff
            }

            element "Container" {
                background #438dd5
                color #ffffff
            }

            element "Database" {
                shape Cylinder
                background #2e7d32
                color #ffffff
            }

            element "Queue" {
                shape Pipe
                background #6a1b9a
                color #ffffff
            }
        }
    }
}
