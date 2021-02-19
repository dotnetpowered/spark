Spark-Lite
==========
Spark Lite is a stripped down fork of Spark meant for buidling a server with minimal dependencies.

It removes from the Spark repo:
- Storage implementations
- Maintenance functions specific to storage implementation
- SignalR 

Spark
=====

Spark is a public domain FHIR server developed in C#, initially built by Firely and as of recently being
maintained by Kufu.

Spark implements a major part of the FHIR specification and has been used and tested during several
HL7 WGM Connectathons.

As of recently the task of maintaining Spark has been taken upon by the community and is led by Kufu.
Kufu and the community, will keep enhancing this server to support the latest versions and add functionality.
We also welcome anyone who wants to support this effort and help us make Spark a better reference
platform and playground for FHIR.

**DISCLAIMER: The web projects Spark.Web and Spark are meant as reference implementations and should never be used out of the box in a production environment without adding as a minimum security features.**

## Versions

#### DSTU1
DSTU1 is no longer maintained by this project. The source code can be found in the branch **dstu1/master**.

#### DSTU2
Source code can be found in the branch **master**, we try to keep up-to-date with the DSTU2 version of FHIR.

#### STU3
Source code can be found in the branch **stu3/master**, we try to keep up-to-date with the STU3 version of FHIR.

#### R4
Source code can be found in the branch **r4/master**. This is the version of Spark running at http://spark.kufu.no
FHIR Endpoint: http://spark.kufu.no/fhir

## Contributing
If you want to contribute, see our [guidelines](https://github.com/furore-fhir/spark/wiki/Contributing)

### Git branching strategy
Our strategy for git branching:

Branch from the master branch which contains the DSTU2 version, unless the feature or bug fix is considered for a specific version of FHIR then branch from either stu3/master or r4/master.

See [GitHub flow](https://guides.github.com/introduction/flow/) for more information.
