# **Senior Full Stack .NET Developer Technical Assessment**

## Sponsorship Request Approval Workflow

Submission Deadline: 4 Days   
Estimated Assessment Duration: 6 hours  
Stack: .NET 8 / .NET 10 (Preferred) \+ React / Angular / Vue / Blazor  
Database: MySQL / PostgreSQL  
Hosting requirement: Candidate must provide a live testing URL

## Assessment Objective

Build a simplified Sponsorship Request Form with an approval workflow.

This assessment evaluates whether the candidate can:

* build a workflow-based enterprise module,  
* implement approval logic,  
* handle role-based access,  
* integrate frontend and backend,  
* design clean backend APIs,  
* and explain architecture decisions clearly.

## Business Scenario

A company receives sponsorship requests from internal staff.

Each request must go through an approval process before it is approved or rejected.

The system should allow:

* request submission,  
* manager approval,  
* finance/admin review,  
* status tracking,  
* and audit history.

## User Roles

| Role | Responsibility |
| :---- | :---- |
| Requestor | Submit sponsorship request |
| Manager | Review and approve/reject request |
| Finance Admin | Final review and approval |
| System Admin | View all requests and manage basic settings |

## Required Workflow

The sponsorship request should follow this flow:

| Status | Description |
| :---- | :---- |
| Draft | Request created but not submitted |
| Pending Manager Approval | Submitted and waiting for manager |
| Pending Finance Review | Manager approved, waiting for finance |
| Approved | Fully approved |
| Rejected | Rejected by manager or finance |
| Cancelled | Cancelled by requestor before approval |

## Sponsorship Request Form Fields

Candidate should implement the following fields:

**Basic Information**

* Request Title  
* Requestor Name  
* Department  
* Sponsorship Type  
* Event / Organisation Name  
* Event Date  
* Requested Amount  
* Purpose / Justification

**Optional Fields**

* Supporting Document Upload  
* Expected Business Benefit  
* Remarks

Document upload can be optional if time is limited.

## Functional Requirements

**1\. Login & Role Access**  
System must support login for:

* Requestor  
* Manager  
* Finance Admin  
* System Admin

Each role should see different actions.

**2\. Requestor Functions**  
Requestor can:

* create sponsorship request,  
* save as draft,  
* submit request,  
* view own requests,  
* cancel request if not yet approved.

**3\. Manager Functions**  
Manager can:

* view pending manager approvals,  
* approve request,  
* reject request,  
* add approval remarks.

If approved, request moves to Pending Finance Review.

**4\. Finance Admin Functions**  
Finance Admin can:

* view requests pending finance review,  
* approve final sponsorship request,  
* reject request,  
* add finance remarks.

If approved, request moves to Approved.

**5\. System Admin Functions**  
System Admin can:

* view all requests,  
* view workflow history,  
* manage sponsorship types.

## Mandatory Deliverables

Candidate must submit:  
**1\. Git Repository**  
Must include:

* source code,  
* proper commits,  
* README.

**2\. Working Application**  
Must include:

* backend API,  
* frontend UI,  
* database setup.

**3\. Setup Guide**

* README should explain:  
* how to run backend,  
* how to run frontend,  
* how to set up database,  
* test login accounts.

**4\. Architecture Explanation**  
Candidate must briefly explain:

* backend architecture,  
* frontend structure,  
* workflow logic,  
* RBAC logic,  
* database design,  
* assumptions/tradeoffs.

**5\. Hosting Requirement**  
The application must be deployed to a live test environment so we can test it directly through the browser.

* Frontend testing URL  
* Backend API URL  
* Swagger / API documentation URL  
* Test login accounts  
* Git repository link  
* Deployment notes

## Important Note to Candidate

This is not expected to be a full production system.

We are assessing:

* how you structure the solution,  
* how you think,  
* how you communicate,  
* how you manage tradeoffs,  
* and how you design workflow-based enterprise software.

If you require more time for the assessment, please kindly email to us for time extension with reason. Our team will decide on a case by case basis.