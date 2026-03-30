// Geolocation utilities for gym check-in functionality

window.geolocation = {
    /**
     * Gets the current position of the user using the browser's Geolocation API
     * @returns {Promise<{latitude: number, longitude: number, accuracy: number}>}
     */
    getCurrentPosition: () => {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation is not supported by this browser'));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                position => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    });
                },
                error => {
                    let errorMessage = 'Failed to get your location: ';
                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            errorMessage += 'Permission denied. Please enable location access for this site.';
                            break;
                        case error.POSITION_UNAVAILABLE:
                            errorMessage += 'Location information is unavailable.';
                            break;
                        case error.TIMEOUT:
                            errorMessage += 'Request timeout. Please try again.';
                            break;
                        default:
                            errorMessage += 'An unknown error occurred.';
                    }
                    reject(new Error(errorMessage));
                },
                {
                    enableHighAccuracy: true,
                    timeout: 15000,
                    maximumAge: 60000 // 1 minute
                }
            );
        });
    },

    /**
     * Calculates the distance between two points on Earth using the Haversine formula
     * @param {number} lat1 - Latitude of first point
     * @param {number} lon1 - Longitude of first point
     * @param {number} lat2 - Latitude of second point
     * @param {number} lon2 - Longitude of second point
     * @returns {number} Distance in meters
     */
    calculateDistance: (lat1, lon1, lat2, lon2) => {
        const R = 6371e3; // Earth's radius in meters
        const φ1 = lat1 * Math.PI / 180; // φ, λ in radians
        const φ2 = lat2 * Math.PI / 180;
        const Δφ = (lat2 - lat1) * Math.PI / 180;
        const Δλ = (lon2 - lon1) * Math.PI / 180;

        const a = Math.sin(Δφ / 2) * Math.sin(Δφ / 2) +
                  Math.cos(φ1) * Math.cos(φ2) *
                  Math.sin(Δλ / 2) * Math.sin(Δλ / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

        return R * c; // Distance in meters
    },

    /**
     * Formats distance for display
     * @param {number} distanceMeters - Distance in meters
     * @returns {string} Formatted distance string
     */
    formatDistance: (distanceMeters) => {
        if (distanceMeters >= 1000) {
            return `${(distanceMeters / 1000).toFixed(1)} km`;
        } else {
            return `${Math.round(distanceMeters)} m`;
        }
    }
};