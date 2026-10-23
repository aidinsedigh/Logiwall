// ---------------------------- Login Models ---------------------
export interface IloginPayload {
  rememberMe?: boolean | undefined;
  userName: string;
  password: string;
  channel?: string | undefined;
  loginProvider?: number | undefined;
}

export interface IloginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

// ------------------------- Otp Models --------------------------

export interface IsendOtpResponse {
  data: boolean;
  message: string;
  succuss: boolean;
}

export interface IconfirmedCodePayload {
  phoneNumber?: string;
  code: string;
  channel?: string;
  loginProvider?: number;
}

export interface IconfirmOtpResponse {
  accessToken: string;
  expiresIn: number;
  fullName: string;
}

// ------------------------------- Register Models ---------------------------------

export interface IregisterPayload {
  confirmPassword: string;
  name: string;
  lastName: string;
  password: string;
  nationalCode: string;
  email?: string;
}
// register response
// -----------------------------------------------------------------------
