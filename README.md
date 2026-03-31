cd FitPlay.Api 

dotnet user-secrets init

dotnet user-secrets set "Stripe:SecretKey" "sk_test_51T7eTnHOGS0NRXI4CHmXp3BD5p9zz8ectEyPXV4327I10cJD7il4tMP6k8PO9upNu8jebTOcW3MmqXoMWeJYnSaT00sSgHUmPm" 

dotnet user-secrets set "Stripe:PublishableKey" "pk_test_51T7eTnHOGS0NRXI4rp5vFJ3VRE6coHU9AiPoYvyQnzhtkCmxQvioGhCzzE2Zk1uDlpOyrkF4EmyrNIio1RveMGcx00UNevRMAo" 

dotnet user-secrets set "Stripe:PriceId" "price_1T7eYYHOGS0NRXI4YLxdTMl3"
