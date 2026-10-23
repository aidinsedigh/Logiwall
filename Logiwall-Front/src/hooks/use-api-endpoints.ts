const useApiEndpoints = () => {
  return Object.freeze({
    GLOBAL: {
      GET_PROFILE_INFO: 'api/Person/GetUserProfile',
      GET_ORIGIN_CITIES_LIST: 'api/Common/GetStaticDatas?Type=11',
      GET_DESTINATION_CITIES_LIST: 'api/Common/GetStaticDatas?Type=12',
    },
    AUTH: {
      POST_LOGIN: '/api/Identity/GetToken',
      POST_SEND_OTP_CODE: '/api/Identity/SendOtpCode',
      POST_CONFIRM_OTP_CODE: '/api/Identity/ConfirmationOtpCode',
      POST_REGISTER: 'api/Person/Register',
    },
    PROFILE: {
      POST_UPDATE_PROFILE: 'api/Person/UpdateUserProfile',
      POST_CHANGE_PASSWORD: 'api/Person/ChangePassword',
    },
    TRACKING: {
      POST_TRACKING_PARCEL: '/odata/Tipax/DspContractTrackings/Tipax.TrackingParcelWithDetail',
    },
    CREATE_ORDER: {
      GET_SERVICES_OPTIONS: '/api/Order/GetOptions',
      POST_SELECT_OPTIONS: '/api/Order/SelectOption',
      POST_GO_PAYMENT: '/api/Order/SelectPaymentType',
    },
    GET_ORDERS: {
      GET_ORDERS_LIST: '/api/Order/GetOrders',
    },
  });
};
export default useApiEndpoints;
