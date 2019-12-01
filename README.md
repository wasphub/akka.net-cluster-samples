# Akka.Net Cluster Samples

  - [Introduction](#introduction)
  - [Structure](#structure)
    - [Main cluster](#main-cluster)
    - [Sub cluster](#sub-cluster)
  - [Structure of the clusters](#structure-of-the-clusters)
    - [General concepts](#general-concepts)
    - [Beacon](#beacon)
    - [Submitter](#submitter)
    - [Worker](#worker)
    - [Watcher](#watcher)
    - [Writer](#writer)
    - [Pub](#pub)
    - [Web](#web)
    - [Sub](#sub)
  - [Run the clusters](#run-the-clusters)
    - [Main cluster](#main-cluster-1)
    - [Sub cluster](#sub-cluster-1)

A sample reference solution to build an [**Akka.Net**](https://getakka.net/index.html)+[**Kafka**](https://kafka.apache.org/) based multi cluster architecture

## Introduction

This repository contains a sample solution where a simple but realistic multi-cluster “architecture” is described. 
It is a trivial sample from a “business logic” point of view, but it is not trivial from an architecture standpoint.

The idea is that clustering and distributed systems in general is an area which evolves continuously, 
and documentation around both principles and tools it is quite scattered. Akka.Net has a very good 
[training repository](https://github.com/petabridge/akka-bootcamp), but it is a bit lacking on the clustering side, while their 
[sample reference architecture](https://github.com/petabridge/akkadotnet-code-samples/tree/master/Cluster.WebCrawler) 
covers clustering much better but is also quite complex, making it not trivial to distillate from it the 
foundational concepts and key bits of code and configuration.

In this context a sample reference solution might help when one starts new stuff or wants to migrate/validate existing work. 
What you will find here is a pragmatic approach, where some choices around tools and patterns are taken and then 
developed into a working example. Such choices might evolve in time, and this is fine, this repository can be kept up to date when such patterns and practices adapt in order to always reflect the shared state of the art.

## Structure of the solution

The trivial business logic is around a trivial task to be performed, which is about receiving input strings and reversing them. In order to simulate a long running job, the task is made artificially long by adding a fixed delay.

This sample solution to resolve this problem has 2 clusters, made of several types of nodes:

### Main cluster
-   Beacon: cluster advertiser
-   Submitter: collects input and sends it out to worker nodes
-   Worker: processes input (it just reverses strings, but it also artificially waits a few seconds to simulate a long job)
-   Watcher: observes output from workers
-   Writer: persists results from workers
-   Web: web-based, real-time mix of watcher and submitter (SignalR)
-   Pub: publishes results into Kafka

### Sub cluster
-   Beacon: cluster advertiser
-   Sub: reads results from Kafka and prints them

All the wiring is done through [routers](https://getakka.net/articles/actors/routers.html) (local and distributed), so that all sorts of nodes can be added dynamically 
and made replicated.

For example, many workers and/or submitters can be added at runtime. Workers just get work to do from submitters 
in a round-robin fashion with a bounded capacity of allowed concurrent work, hence demonstrating horizontal scalability 
by just spinning new workers.

At the same time watchers can also be added at will, however they receive messages 
in a broadcast fashion.

All this is pretty much done through configuration and very little coding.

## Structure of the clusters

The following sections will be very brief and assume at least some basic knowledge of both Akka.Net and Kafka. They will focus mainly on the dynamics of this example, describing what we are trying to achieve and how we approach the various problems.

### General concepts

A [_cluster_](https://getakka.net/articles/clustering/cluster-overview.html) represents a fault-tolerant, elastic, decentralized peer-to-peer network of Akka.NET applications (_nodes_) with no single point of failure or bottleneck. A cluster is a highly dynamic structure: nodes can either join or leave the cluster whenever they want. Also in case of failing nodes, the cluster structure is able to detect such cases and apply strategies to remain consistent and responsive.

Nodes are labelled with _roles_, which among other things can be leveraged to assign declarative behaviors through configuration.

There is no concept of _master_ node: in a cluster all nodes are the same. A cluster stays alive thanks to a mechanism based on a protocol called _gossip_. Following such protocol, node continuously exchange gossip messages about what other nodes they can reach, and a consensus logic make it so that they constantly reach agreement about what nodes are available in the cluster.

The rest of the discussion assumes that the solution is configured as a fully clustered system. Code samples also show how to run things with fewer nodes, but those setups are not necessarily discussed here.

### Beacon

While an Akka.Net cluster does not need any master role, the concept of a _seed node_ is introduced to make the cluster discoverable. Normally a cluster contains one or more seed nodes, which have the only task of "being there" and being reachable from other nodes in order to let them join the cluster.

Once the cluster is formed, seed node are not formally needed to let other nodes perform their duties, but of course their presence becomes important in case more nodes need to join. For this reason this example contains a process called **Beacon**, whose role is to fulfill the role of a seed node. Both clusters in this example are configured to have one seed node, but this can be changed easily at configuration level in case seed nodes need to be made redundant.

Note: Beacon supports [**Petabridge.Cmd**](https://cmd.petabridge.com/articles/index.html). You can connect to it using `pbm` like this

```cmd
pbm localhost:9110
```

### Submitter

A **Submitter** is a process which just collects input strings from the user and sends them to a worker actor for processing. During that action it also collects a reference to a watcher actor and sends it to the worker to inform it about the destination of the result.

Reference to workers and watchers can be created to be both local and remote. When remote, they are retrieved from configuration and instantiated as _routers_. Routers will transparently take care of forwarding messages to the right nodes according to the cluster structure, and within them to the right actors. Routers can be configured to behave according to several different _routing strategies_. 

Multiple submitters can join the cluster.

### Worker

A **worker** node instantiates one actor, whose sole goal is to fulfill computation requests and send the result to the watcher reference it received within the command. Multiple workers can be added to the cluster as long as configuration is adapted accordingly. This simulates a situation where computational load needs to be distributed for horizontal scalability.

### Watcher

A **watcher** node instantiates one actor, whose sole goal is to display results received by a worker. Multiple watchers can be added to the cluster as long as configuration is adapted accordingly. This simulates a situation where multiple counterparts are interested in the results coming out from workers.

### Writer

A **writer** node is a special type of watcher, in fact its configuration in the example assigns to it the _watcher_ role. As any watcher, it will receive all results from workers, but this type of node will host an actor which will just persists them to some storage. This done through the fact that the actor is declared as [**persistent**](https://getakka.net/articles/persistence/architecture.html).

This particular type of node shows how roles are decoupled from the actual type of processing of the corresponding nodes. Roles are just declarations of intent, which allow us to have processes plugged into specific places of our clustered workflows, regardless of what actions they actually take afterwards.

In this example we assume a Sql Server based persistence, and you can use the `sqlserver_persistence_schema.sql` file to create the righrt schema to the destination database. Check `akka.hocon` to verify, and maybe adjust, the name of both the database and the database server.

### Pub

A **pub** node is a yet another special type of watcher, also in this case its configuration in the example assigns to it the _watcher_ role. As any watcher, it will receive all results from workers, but this type of node will just publish them to a specific topic on a Kafka log instance (in our example we simulate it using Event Hubs on Azure).

### Web

A **Web** node is a web application which joins the cluster to supply another type of **Watcher**. At the same time it is able to submit tasks to the cluster, hence it is tagged as both _subscriber_ and _watcher_, although the former is not formally needed by this example in its current form. 

### Sub

A **sub** node belongs to the **Sub** cluster. While all other nodes seen so far were part of the **Main** cluster, where all the action happens, we wanted to illustrate how sometimes you might need to have _decoupled_ cluster, which are still able to communicate but have independent structures and lifetimes. Kafka is a great way to achieve such independence, much like any type of _message bus_ architecture, and this example shows how Akka.Net can be used to build connected but still independent cluster. Of course the **Sub** node could even be built on a completely different technology unrelated to either Akka.Net or to any kind of distributed system architecture.

In our example we instantiate a separate **Beacon** node for the **Sub** cluster, and then we have the **Sub** node join it. Inside this node a single actor will start consuming messages from Kafka sent to the same topic used by the **Pub** node, hence reacting very quickly to results published on the main cluster.

## Run the clusters

### Main cluster

1. Launch `beacon_main.cmd` to start its seed node.

2. Launch `submitter.cmd` to have a client able to launch tasks. You can also launch it multiple times.
    
    Looking at `Program.cs` of the `Submitter` project, you will see that you can pick 3 different setups. Uncomment the one you want to simulate, compile the project and launch `submitter.cmd`. If you pick the _local_ case you will have a standalone, non-clustered application, otherwise you will start seeing how you can leverage the clustered scenario.

1. Launch `worker.cmd` to have a node able to reverse strings. You can also launch it multiple times.

1. Launch `watcher.cmd` to have a node able to display execution results. You can also launch it multiple times.

    This type of node is not mandatory to have strings reversed, but without it you would not see results in a fully remote setup.

1. (optional) Launch `writer.cmd` to have results persisted across cluster executions. **This node must be launched only once**.

1. (optional) Launch `pub.cmd` to have results published into the Kafka log. **This node should be launched only once** to avoid duplicate messages.

1. (optional) Launch the Web application to have a web client able to both submit tasks and display results from any submitter. You can also launch it multiple times.

### Sub cluster

1. Launch `beacon_sub.cmd` to start its seed node.

1. Launch `sub.cmd` to have results extracted from the Kafka log in "real time" and displayed. You could launch this node it multiple times, but to avoid inconsistent behaviors with Kafka code should be adjusted to correctly use Kafka consumer groups.
