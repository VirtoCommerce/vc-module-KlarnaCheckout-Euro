# Klarna Checkout Europe payment gateway module
Klarna Checkout Europe payment gateway module provides integration with <a href="https://www.klarna.com" target="_blank">Klarna Checkout Europe</a> payment gateway. 

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Klarna Checkout Europe payment gateway -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-KlarnaCheckout-Euro/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install.

# Settings
* **Application key** - Klarna application key from credentials
* **Secret key** - Klarna secret key from credentials
* **Working mode** - Mode of Klarna payment gateway (test or real)
* **Terms Url** - Klarna Terms Url
* **Checkout Url** - Klarna Checkout Url
* **Confirmation Url** - Klarna Confirmation Url
* **Payment action type** - Action type for payment
* **Purchase country two letter code** - Purchase country two letter code used for creating payment, it must be exact as your chosen currency and locale
* **Purchase currency three letter code** - Purchase currency three letter code used for creating payment, it must be chosen with country and locale
* **Locale** - Purchase locale code used for creating payment, it must be chosen with currency and country


# License
Copyright (c) Virtosoftware Ltd.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
