/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

/** Data Transfer Object for Book entity returned from the API */
export interface BookDto {
  /** @format int32 */
  id?: number;
  isbn?: string | null;
  title?: string | null;
  subtitle?: string | null;
  description?: string | null;
  publisher?: string | null;
  /** @format date-time */
  publishedDate?: string | null;
  /** @format int32 */
  pageCount?: number | null;
  language?: string | null;
  /** @format int32 */
  categoryId?: number;
  categoryName?: string | null;
  /** @format int32 */
  totalCopies?: number;
  /** @format int32 */
  availableCopies?: number;
  shelfLocation?: string | null;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  updatedAt?: string;
}

/** Standard API response wrapper for consistent error handling */
export interface BookDtoApiResponse {
  success?: boolean;
  /** Data Transfer Object for Book entity returned from the API */
  data?: BookDto;
  message?: string | null;
  errors?: string[] | null;
}

/** Standard API response wrapper for consistent error handling */
export interface BookDtoListApiResponse {
  success?: boolean;
  data?: BookDto[] | null;
  message?: string | null;
  errors?: string[] | null;
}

/** Paginated response wrapper for list endpoints */
export interface BookDtoPaginatedResponse {
  data?: BookDto[] | null;
  /** @format int32 */
  page?: number;
  /** @format int32 */
  pageSize?: number;
  /** @format int32 */
  totalCount?: number;
  /** @format int32 */
  totalPages?: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}

/** Data Transfer Object for Category entity */
export interface CategoryDto {
  /** @format int32 */
  id?: number;
  name?: string | null;
  description?: string | null;
  /** @format date-time */
  createdAt?: string;
  /** @format date-time */
  updatedAt?: string;
}

/** Standard API response wrapper for consistent error handling */
export interface CategoryDtoApiResponse {
  success?: boolean;
  /** Data Transfer Object for Category entity */
  data?: CategoryDto;
  message?: string | null;
  errors?: string[] | null;
}

/** Standard API response wrapper for consistent error handling */
export interface CategoryDtoListApiResponse {
  success?: boolean;
  data?: CategoryDto[] | null;
  message?: string | null;
  errors?: string[] | null;
}

/** Request model for creating a new book */
export interface CreateBookRequest {
  /**
   * @minLength 1
   * @pattern ^\d{10}(\d{3})?$
   */
  isbn: string;
  /**
   * @minLength 0
   * @maxLength 200
   */
  title: string;
  /**
   * @minLength 0
   * @maxLength 200
   */
  subtitle?: string | null;
  /**
   * @minLength 0
   * @maxLength 2000
   */
  description?: string | null;
  /**
   * @minLength 0
   * @maxLength 100
   */
  publisher?: string | null;
  /** @format date-time */
  publishedDate?: string | null;
  /**
   * @format int32
   * @min 1
   * @max 10000
   */
  pageCount?: number | null;
  /**
   * @minLength 0
   * @maxLength 50
   */
  language?: string | null;
  /**
   * @format int32
   * @min 1
   * @max 2147483647
   */
  categoryId: number;
  /**
   * @format int32
   * @min 0
   * @max 1000
   */
  totalCopies: number;
  /**
   * @minLength 0
   * @maxLength 50
   */
  shelfLocation?: string | null;
}

/** Request model for creating a new category */
export interface CreateCategoryRequest {
  /**
   * @minLength 0
   * @maxLength 50
   */
  name: string;
  /**
   * @minLength 0
   * @maxLength 500
   */
  description?: string | null;
}

/** Standard API response wrapper for consistent error handling */
export interface ObjectApiResponse {
  success?: boolean;
  data?: any;
  message?: string | null;
  errors?: string[] | null;
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

/** Request model for updating an existing book */
export interface UpdateBookRequest {
  /**
   * @minLength 0
   * @maxLength 200
   */
  title?: string | null;
  /**
   * @minLength 0
   * @maxLength 200
   */
  subtitle?: string | null;
  /**
   * @minLength 0
   * @maxLength 2000
   */
  description?: string | null;
  /**
   * @minLength 0
   * @maxLength 100
   */
  publisher?: string | null;
  /** @format date-time */
  publishedDate?: string | null;
  /**
   * @format int32
   * @min 1
   * @max 10000
   */
  pageCount?: number | null;
  /**
   * @minLength 0
   * @maxLength 50
   */
  language?: string | null;
  /**
   * @format int32
   * @min 1
   * @max 2147483647
   */
  categoryId?: number | null;
  /**
   * @format int32
   * @min 0
   * @max 1000
   */
  totalCopies?: number | null;
  /**
   * @minLength 0
   * @maxLength 50
   */
  shelfLocation?: string | null;
}

/** Request model for updating an existing category */
export interface UpdateCategoryRequest {
  /**
   * @minLength 0
   * @maxLength 50
   */
  name?: string | null;
  /**
   * @minLength 0
   * @maxLength 500
   */
  description?: string | null;
}

import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  HeadersDefaults,
  ResponseType,
} from "axios";
import axios from "axios";

export type QueryParamsType = Record<string | number, any>;

export interface FullRequestParams
  extends Omit<AxiosRequestConfig, "data" | "params" | "url" | "responseType"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseType;
  /** request body */
  body?: unknown;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown>
  extends Omit<AxiosRequestConfig, "data" | "cancelToken"> {
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<AxiosRequestConfig | void> | AxiosRequestConfig | void;
  secure?: boolean;
  format?: ResponseType;
}

export enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public instance: AxiosInstance;
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private secure?: boolean;
  private format?: ResponseType;

  constructor({
    securityWorker,
    secure,
    format,
    ...axiosConfig
  }: ApiConfig<SecurityDataType> = {}) {
    this.instance = axios.create({
      ...axiosConfig,
      baseURL: axiosConfig.baseURL || "",
    });
    this.secure = secure;
    this.format = format;
    this.securityWorker = securityWorker;
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected mergeRequestParams(
    params1: AxiosRequestConfig,
    params2?: AxiosRequestConfig,
  ): AxiosRequestConfig {
    const method = params1.method || (params2 && params2.method);

    return {
      ...this.instance.defaults,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...((method &&
          this.instance.defaults.headers[
            method.toLowerCase() as keyof HeadersDefaults
          ]) ||
          {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected stringifyFormItem(formItem: unknown) {
    if (typeof formItem === "object" && formItem !== null) {
      return JSON.stringify(formItem);
    } else {
      return `${formItem}`;
    }
  }

  protected createFormData(input: Record<string, unknown>): FormData {
    if (input instanceof FormData) {
      return input;
    }
    return Object.keys(input || {}).reduce((formData, key) => {
      const property = input[key];
      const propertyContent: any[] =
        property instanceof Array ? property : [property];

      for (const formItem of propertyContent) {
        const isFileType = formItem instanceof Blob || formItem instanceof File;
        formData.append(
          key,
          isFileType ? formItem : this.stringifyFormItem(formItem),
        );
      }

      return formData;
    }, new FormData());
  }

  public request = async <T = any, _E = any>({
    secure,
    path,
    type,
    query,
    format,
    body,
    ...params
  }: FullRequestParams): Promise<AxiosResponse<T>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const responseFormat = format || this.format || undefined;

    if (
      type === ContentType.FormData &&
      body &&
      body !== null &&
      typeof body === "object"
    ) {
      body = this.createFormData(body as Record<string, unknown>);
    }

    if (
      type === ContentType.Text &&
      body &&
      body !== null &&
      typeof body !== "string"
    ) {
      body = JSON.stringify(body);
    }

    return this.instance.request({
      ...requestParams,
      headers: {
        ...(requestParams.headers || {}),
        ...(type ? { "Content-Type": type } : {}),
      },
      params: query,
      responseType: responseFormat,
      data: body,
      url: path,
    });
  };
}

/**
 * @title Library Management API
 * @version v1
 * @contact Library Management System <admin@library.com>
 *
 * ASP.NET Core Web API for managing library books and categories using ADO.NET
 */
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  api = {
    /**
     * No description
     *
     * @tags Books
     * @name BooksList
     * @summary Get paginated list of books with optional filtering
     * @request GET:/api/Books
     */
    booksList: (
      query?: {
        /**
         * Page number (default: 1)
         * @format int32
         * @default 1
         */
        page?: number;
        /**
         * Items per page (default: 10, max: 100)
         * @format int32
         * @default 10
         */
        pageSize?: number;
        /**
         * Filter by category ID
         * @format int32
         */
        categoryId?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<BookDtoPaginatedResponse, ProblemDetails | void>({
        path: `/api/Books`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Books
     * @name BooksCreate
     * @summary Create a new book
     * @request POST:/api/Books
     */
    booksCreate: (data: CreateBookRequest, params: RequestParams = {}) =>
      this.request<BookDtoApiResponse, ProblemDetails | void>({
        path: `/api/Books`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Books
     * @name BooksDetail
     * @summary Get a specific book by ID
     * @request GET:/api/Books/{id}
     */
    booksDetail: (id: number, params: RequestParams = {}) =>
      this.request<BookDtoApiResponse, ProblemDetails | void>({
        path: `/api/Books/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Books
     * @name BooksUpdate
     * @summary Update an existing book
     * @request PUT:/api/Books/{id}
     */
    booksUpdate: (
      id: number,
      data: UpdateBookRequest,
      params: RequestParams = {},
    ) =>
      this.request<BookDtoApiResponse, ProblemDetails | void>({
        path: `/api/Books/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Books
     * @name BooksDelete
     * @summary Delete a book (soft delete)
     * @request DELETE:/api/Books/{id}
     */
    booksDelete: (id: number, params: RequestParams = {}) =>
      this.request<ObjectApiResponse, ProblemDetails | void>({
        path: `/api/Books/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Books
     * @name BooksSearchList
     * @summary Search for books by title
     * @request GET:/api/Books/search
     */
    booksSearchList: (
      query?: {
        /** Search query */
        query?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<BookDtoListApiResponse, ProblemDetails | void>({
        path: `/api/Books/search`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Books
     * @name BooksCategoryList
     * @summary Get the category of a specific book
     * @request GET:/api/Books/{id}/category
     */
    booksCategoryList: (id: number, params: RequestParams = {}) =>
      this.request<CategoryDtoApiResponse, ProblemDetails | void>({
        path: `/api/Books/${id}/category`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Categories
     * @name CategoriesList
     * @summary Get all categories
     * @request GET:/api/Categories
     */
    categoriesList: (params: RequestParams = {}) =>
      this.request<CategoryDtoListApiResponse, void>({
        path: `/api/Categories`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Categories
     * @name CategoriesCreate
     * @summary Create a new category
     * @request POST:/api/Categories
     */
    categoriesCreate: (
      data: CreateCategoryRequest,
      params: RequestParams = {},
    ) =>
      this.request<CategoryDtoApiResponse, ProblemDetails | void>({
        path: `/api/Categories`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Categories
     * @name CategoriesDetail
     * @summary Get a specific category by ID
     * @request GET:/api/Categories/{id}
     */
    categoriesDetail: (id: number, params: RequestParams = {}) =>
      this.request<CategoryDtoApiResponse, ProblemDetails | void>({
        path: `/api/Categories/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Categories
     * @name CategoriesUpdate
     * @summary Update an existing category
     * @request PUT:/api/Categories/{id}
     */
    categoriesUpdate: (
      id: number,
      data: UpdateCategoryRequest,
      params: RequestParams = {},
    ) =>
      this.request<CategoryDtoApiResponse, ProblemDetails | void>({
        path: `/api/Categories/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Categories
     * @name CategoriesDelete
     * @summary Delete a category
     * @request DELETE:/api/Categories/{id}
     */
    categoriesDelete: (id: number, params: RequestParams = {}) =>
      this.request<ObjectApiResponse, ProblemDetails | void>({
        path: `/api/Categories/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),
  };
}
