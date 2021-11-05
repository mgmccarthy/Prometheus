# NServicebus-Prometheus-Granfana
- Prometheus Getting Started Tutorial: https://prometheus.io/docs/prometheus/latest/getting_started/
- Granfana Introduction Tutorial: https://grafana.com/tutorials/grafana-fundamentals/
- Visual Studio Solution and NSB tutorial was downloaded from here: https://docs.particular.net/samples/logging/prometheus-grafana/#prometheus
- grafana-endpoints-dashboard.json downloaded from here: https://docs.particular.net/samples/logging/prometheus-grafana/grafana-endpoints-dashboard.json

# Links to Running Services
- Prometheus: http://localhost:9090/
- Promethues Targets: http://localhost:9090/targets
    - list of endpoints and their health status
- Prometheus Metrics: http://localhost:9090/metrics
- NServiceBus/Prometheus MetricServer metrics: http://localhost:3030/metrics
- Grafana: http://127.0.0.1:3000/login

# Setup
In the root fo the repo is a docker-compose.yml file. This file holds the container start up for Prometheus and Granfana. I commented out the #app: application, as that shipped with the original Prometheus tutorial and is not needed for the NSB example. Usage is
```
docker-compose up
```

to compose all container up
```
docker-compose down -v (-v optional to delete all volumes created)
```

In the `/promethus` folder, you'll see two files:
- promethus.yml: this is the prometheus configuration file. The last `static-configs` section for `-targets` is commented out. Why? If you're running Prometheus in a docker conatiner, it can't resolve `localhost:3030`, so instead, you have to use `host.docker.internal:3030`
- nservicebus.rules.txt: these are the metrics "rules". This .txt file loaded by prometheus.yml

# Usage
Follow these steps to make sure all containers and services in containers have stood up and are operting correclty. You'll need to do some Grafana work to import the Prometheus data source and then create a dashboard for the NSB metrics.

- Run `docker-compose up`. add '`-d` for detached if you don't want console output, but console output can be helpful to see if all containers stood up as well as problems when the container's services urls are accessed. 
- After compose up, start the NSB project. This will start up the Prometheus metrics server on port 3030.
- Go to the following URL's to check the health of all running services:
  - http://localhost:9090/
  - http://localhost:9090/targets
    - on this page, both 9090 and 3030 should be GREEN/HEALTHY
  - http://localhost:3030/metrics
  - http://localhost:9090/metrics
- In Grafana, log in as "admin/admin", and Skip the password reset step
- Add Promethus as a data source to Grafana (steps on how to do that are in the "Add a metrics data source section here: https://grafana.com/tutorials/grafana-fundamentals/)
- Pick up the NSB tutorial at this step: https://docs.particular.net/samples/logging/prometheus-grafana/#prometheus-show-a-graph
- For the Grafana configuration, start with the Manual configuration step (https://docs.particular.net/samples/logging/prometheus-grafana/#grafana-manual-configuration)
- Add a dashboard/import a dashboard in Grafana using the the grafana-endpoints-dashboard.json file in the root of this repo
- After import, you should see the NSB metrics in Grafana
